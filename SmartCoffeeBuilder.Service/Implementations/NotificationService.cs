using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Responses.Notification;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Notifications;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Quản lý thông báo in-app + email. Mỗi noti được lưu vào bảng notifications (lịch sử cho
/// FE/mobile) rồi cố gắng gửi email tương ứng. Lỗi gửi email KHÔNG làm hỏng hành động nghiệp vụ
/// (apply/accept/reject) — bản ghi vẫn được lưu với EmailSentAt = null để có thể resend.
///
/// KHÁC với OtpService (OTP tài khoản) — đây là hệ thống noti riêng, chỉ tái dùng EmailService.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Notification> _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Notification>();
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PaginationResponse<NotificationResponse>> GetForAccountAsync(
        long accountId, int pageNumber = 1, int pageSize = 20, bool? isRead = null)
    {
        var query = _repository
            .GetQueryable(n => n.AccountId == accountId && (isRead == null || n.IsRead == isRead))
            .OrderByDescending(n => n.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<NotificationResponse>(
            paged.Items.Select(NotificationResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<NotificationResponse> GetByIdAsync(long id)
    {
        var noti = await _repository.SingleOrDefaultAsync(predicate: n => n.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy notification với id {id}.");

        return NotificationResponse.From(noti);
    }

    public Task<int> GetUnreadCountAsync(long accountId) =>
        _repository.CountAsync(n => n.AccountId == accountId && !n.IsRead);

    public async Task<NotificationResponse> MarkAsReadAsync(long id)
    {
        var noti = await _repository.SingleOrDefaultAsync(predicate: n => n.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy notification với id {id}.");

        if (!noti.IsRead)
        {
            noti.IsRead = true;
            _repository.Update(noti);
            await _unitOfWork.CommitAsync();
        }

        return NotificationResponse.From(noti);
    }

    public async Task<int> MarkAllAsReadAsync(long accountId)
    {
        var unread = await _repository.GetListAsync(
            predicate: n => n.AccountId == accountId && !n.IsRead);

        if (unread.Count == 0) return 0;

        foreach (var noti in unread) noti.IsRead = true;
        _repository.UpdateRange(unread);
        await _unitOfWork.CommitAsync();

        return unread.Count;
    }

    public async Task<NotificationResponse> ResendAsync(long id)
    {
        var noti = await _repository.SingleOrDefaultAsync(
            predicate: n => n.Id == id,
            include: q => q.Include(n => n.Account))
            ?? throw new KeyNotFoundException($"Không tìm thấy notification với id {id}.");

        var email = noti.Account?.Email;
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Account của notification không có email để gửi lại.");

        // Resend là hành động chủ động — để lỗi email nổi lên (500) cho người gọi biết.
        await SendEmailAsync(email, noti.Type, noti.Title, noti.Content);

        noti.EmailSentAt = DateTime.UtcNow;
        _repository.Update(noti);
        await _unitOfWork.CommitAsync();

        return NotificationResponse.From(noti);
    }

    // ──────────────────────────────── Domain triggers ────────────────────────────────

    public async Task NotifyApplicationReceivedAsync(long applicationId)
    {
        var app = await _unitOfWork.GetRepository<Apply>().SingleOrDefaultAsync(
            predicate: a => a.Id == applicationId,
            include: q => q
                .Include(a => a.Post).ThenInclude(p => p.ProjectShopOwner).ThenInclude(pr => pr.Owner).ThenInclude(o => o.Account)
                .Include(a => a.ServiceProviderProfile));

        var ownerAccount = app?.Post?.ProjectShopOwner?.Owner?.Account;
        if (app is null || ownerAccount is null)
        {
            _logger.LogWarning("Bỏ qua noti application_received: không resolve được owner cho application #{Id}.", applicationId);
            return;
        }

        var providerName = app.ServiceProviderProfile?.DisplayName ?? "Một nhà cung cấp";
        var projectName = app.Post?.ProjectShopOwner?.Name ?? "dự án của bạn";
        var postTitle = app.Post?.Title ?? "bài đăng";

        var title = "Hồ sơ ứng tuyển mới cho dự án của bạn";
        var content = $"Nhà cung cấp \"{providerName}\" vừa ứng tuyển vào bài đăng \"{postTitle}\" " +
                      $"thuộc dự án \"{projectName}\". Vui lòng xem xét và phản hồi hồ sơ.";

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, NotificationTypes.ApplicationReceived,
            title, content, referenceType: "project_application", referenceId: app.Id);
    }

    public async Task NotifyApplicationDecisionAsync(long applicationId, bool accepted)
    {
        var app = await _unitOfWork.GetRepository<Apply>().SingleOrDefaultAsync(
            predicate: a => a.Id == applicationId,
            include: q => q
                .Include(a => a.Post).ThenInclude(p => p.ProjectShopOwner)
                .Include(a => a.ServiceProviderProfile).ThenInclude(pr => pr.Account));

        var providerAccount = app?.ServiceProviderProfile?.Account;
        if (app is null || providerAccount is null)
        {
            _logger.LogWarning("Bỏ qua noti application decision: không resolve được provider cho application #{Id}.", applicationId);
            return;
        }

        var projectName = app.Post?.ProjectShopOwner?.Name ?? "dự án";
        var postTitle = app.Post?.Title ?? "bài đăng";

        string type, title, content;
        if (accepted)
        {
            type = NotificationTypes.ApplicationAccepted;
            title = "Hồ sơ ứng tuyển của bạn đã được chấp nhận";
            content = $"Chúc mừng! Hồ sơ ứng tuyển của bạn cho bài đăng \"{postTitle}\" (dự án \"{projectName}\") " +
                      "đã được chủ quán chấp nhận. Hai bên có thể bắt đầu triển khai công việc.";
        }
        else
        {
            type = NotificationTypes.ApplicationRejected;
            title = "Hồ sơ ứng tuyển của bạn chưa được chọn";
            content = $"Rất tiếc, hồ sơ ứng tuyển của bạn cho bài đăng \"{postTitle}\" (dự án \"{projectName}\") " +
                      "chưa được chọn lần này. Cảm ơn bạn đã quan tâm.";
        }

        await CreateAndDispatchAsync(
            providerAccount.Id, providerAccount.Email, type,
            title, content, referenceType: "project_application", referenceId: app.Id);
    }

    public async Task NotifyEngagementInvitedAsync(long projectWorkingId)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        var providerAccount = engagement?.ServiceProviderProfile?.Account;
        if (engagement is null || providerAccount is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti engagement_invited: không resolve được provider cho engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var shopName = engagement.ProjectShopOwner?.Owner?.ShopName;
        var ownerLabel = string.IsNullOrWhiteSpace(shopName) ? "Một chủ quán" : $"\"{shopName}\"";
        var projectName = engagement.ProjectShopOwner?.Name ?? "một dự án";

        var note = string.IsNullOrWhiteSpace(engagement.RequestMessage)
            ? string.Empty
            : $" Lời nhắn: \"{engagement.RequestMessage}\".";

        await CreateAndDispatchAsync(
            providerAccount.Id, providerAccount.Email, NotificationTypes.EngagementInvited,
            title: "Bạn nhận được lời mời hợp tác trực tiếp",
            content: $"{ownerLabel} vừa mời bạn hợp tác ({engagement.ContractType}) cho dự án \"{projectName}\".{note} " +
                     "Vui lòng phản hồi (nhận hoặc từ chối) lời mời.",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementInviteDecisionAsync(long projectWorkingId, bool accepted)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        var ownerAccount = engagement?.ProjectShopOwner?.Owner?.Account;
        if (engagement is null || ownerAccount is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti engagement_invite decision: không resolve được owner cho engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var providerName = engagement.ServiceProviderProfile?.DisplayName ?? "Nhà cung cấp";
        var projectName = engagement.ProjectShopOwner?.Name ?? "dự án của bạn";

        string type, title, content;
        if (accepted)
        {
            type = NotificationTypes.EngagementInviteAccepted;
            title = "Nhà cung cấp đã nhận lời mời hợp tác";
            content = $"\"{providerName}\" đã đồng ý lời mời hợp tác ({engagement.ContractType}) " +
                      $"cho dự án \"{projectName}\". Hai bên có thể bắt đầu (ký hợp đồng, khảo sát...).";
        }
        else
        {
            type = NotificationTypes.EngagementInviteRejected;
            title = "Nhà cung cấp đã từ chối lời mời hợp tác";
            content = $"\"{providerName}\" đã từ chối lời mời hợp tác ({engagement.ContractType}) " +
                      $"cho dự án \"{projectName}\". Bạn có thể mời một nhà cung cấp khác.";
        }

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, type, title, content,
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementCompletionRequestedAsync(long projectWorkingId)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        var ownerAccount = engagement?.ProjectShopOwner?.Owner?.Account;
        if (engagement is null || ownerAccount is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti engagement_completion_requested: không resolve được owner cho engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var providerName = engagement.ServiceProviderProfile?.DisplayName ?? "Nhà cung cấp";
        var projectName = engagement.ProjectShopOwner?.Name ?? "dự án của bạn";

        var note = string.IsNullOrWhiteSpace(engagement.CompletionRequestNote)
            ? string.Empty
            : $" Ghi chú bàn giao: \"{engagement.CompletionRequestNote}\".";

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, NotificationTypes.EngagementCompletionRequested,
            title: "Nhà cung cấp báo hoàn thành, chờ bạn nghiệm thu",
            content: $"\"{providerName}\" vừa báo đã hoàn thành phần việc ({engagement.ContractType}) " +
                     $"thuộc dự án \"{projectName}\".{note} Vui lòng kiểm tra và bấm nghiệm thu để hoàn tất hợp tác.",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementCompletedAsync(long projectWorkingId)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        var providerAccount = engagement?.ServiceProviderProfile?.Account;
        if (engagement is null || providerAccount is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti engagement_completed: không resolve được provider cho engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var projectName = engagement.ProjectShopOwner?.Name ?? "dự án";

        await CreateAndDispatchAsync(
            providerAccount.Id, providerAccount.Email, NotificationTypes.EngagementCompleted,
            title: "Công việc của bạn đã được nghiệm thu",
            content: $"Chủ quán đã nghiệm thu phần việc ({engagement.ContractType}) của bạn tại dự án " +
                     $"\"{projectName}\". Hợp tác hoàn tất — chủ quán có thể gửi đánh giá cho bạn từ lúc này.",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementTerminatedAsync(long projectWorkingId, bool terminatedByOwner)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        if (engagement is null)
        {
            _logger.LogWarning("Bỏ qua noti engagement_terminated: không tìm thấy engagement #{Id}.", projectWorkingId);
            return;
        }

        // Người huỷ đã biết rồi — chỉ báo cho bên còn lại.
        var recipient = terminatedByOwner
            ? engagement.ServiceProviderProfile?.Account
            : engagement.ProjectShopOwner?.Owner?.Account;
        if (recipient is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti engagement_terminated: không resolve được người nhận cho engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var projectName = engagement.ProjectShopOwner?.Name ?? "dự án";
        var actor = terminatedByOwner ? "Chủ quán" : "Nhà cung cấp";

        await CreateAndDispatchAsync(
            recipient.Id, recipient.Email, NotificationTypes.EngagementTerminated,
            title: "Hợp tác đã bị huỷ ngang",
            content: $"{actor} đã huỷ ngang hợp tác ({engagement.ContractType}) tại dự án \"{projectName}\". " +
                     "Các công việc liên quan của hợp tác này dừng lại từ thời điểm hiện tại.",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyProjectReadyToCloseAsync(long projectShopOwnerId)
    {
        var project = await _unitOfWork.GetRepository<ProjectShopOwner>().SingleOrDefaultAsync(
            predicate: p => p.Id == projectShopOwnerId && p.DeletedAt == null,
            include: q => q.Include(p => p.Owner).ThenInclude(o => o.Account)
                           .Include(p => p.ProjectWorkings));
        if (project is null) return;

        // Dự án đã đóng/huỷ rồi thì không còn gì để nhắc.
        if (project.Status is not (ProjectStatus.briefed or ProjectStatus.in_progress)) return;

        // Cùng bộ điều kiện với ProjectShopOwnerService.CompleteAsync: không còn hợp tác dang dở,
        // và có ít nhất một hợp tác đã nghiệm thu. Chưa đủ thì im lặng — caller không cần biết.
        var openCount = project.ProjectWorkings.Count(
            e => e.Status is ProviderStatus.requested or ProviderStatus.accepted);
        if (openCount > 0) return;

        var completedCount = project.ProjectWorkings.Count(e => e.Status == ProviderStatus.completed);
        if (completedCount == 0) return;

        var ownerAccount = project.Owner?.Account;
        if (ownerAccount is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti project_ready_to_close: không resolve được owner cho dự án #{Id}.",
                projectShopOwnerId);
            return;
        }

        // KHÔNG chặn theo "đã có noti chưa đọc": owner có thể mời thêm provider sau khi được nhắc,
        // lúc đó lời nhắc cũ thành sai và phải có lời nhắc mới khi hợp tác mới khép lại. Mỗi lần
        // gửi ứng với đúng một lần dự án chuyển sang trạng thái đóng được, nên không sinh trùng.
        var plural = completedCount > 1 ? $"cả {completedCount} hợp tác" : "hợp tác";

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, NotificationTypes.ProjectReadyToClose,
            title: "Dự án đã xong, chờ bạn đóng",
            content: $"Dự án \"{project.Name}\" đã nghiệm thu xong {plural} và không còn hợp tác nào " +
                     "đang chạy. Vào dự án bấm \"Hoàn thành dự án\" để đóng lại và kết thúc.",
            referenceType: ProjectReference, referenceId: projectShopOwnerId);
    }

    public async Task NotifyProjectClosedAsync(
        long projectShopOwnerId, bool cancelled, IReadOnlyCollection<long> affectedProjectWorkingIds)
    {
        if (affectedProjectWorkingIds.Count == 0) return;

        var project = await _unitOfWork.GetRepository<ProjectShopOwner>()
            .SingleOrDefaultAsync(predicate: p => p.Id == projectShopOwnerId);
        var projectName = project?.Name ?? "dự án";

        var engagements = await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
            predicate: e => affectedProjectWorkingIds.Contains(e.Id),
            include: q => q.Include(e => e.ServiceProviderProfile).ThenInclude(p => p.Account));

        var type = cancelled ? NotificationTypes.ProjectCancelled : NotificationTypes.ProjectCompleted;
        var title = cancelled ? "Dự án đã bị huỷ" : "Dự án đã hoàn thành";

        // Một provider có thể có nhiều engagement trong cùng dự án — chỉ gửi 1 noti cho mỗi tài khoản.
        var recipients = engagements
            .Select(e => e.ServiceProviderProfile?.Account)
            .Where(a => a != null)
            .GroupBy(a => a!.Id)
            .Select(g => g.First()!);

        foreach (var account in recipients)
        {
            var content = cancelled
                ? $"Chủ quán đã huỷ dự án \"{projectName}\". Các hợp tác đang mở của bạn tại dự án này đã được đóng lại."
                : $"Dự án \"{projectName}\" đã được chủ quán đóng và hoàn thành. Cảm ơn bạn đã đồng hành.";

            await CreateAndDispatchAsync(
                account.Id, account.Email, type, title, content,
                referenceType: ProjectReference, referenceId: projectShopOwnerId);
        }
    }

    // ──────────────────────────────── Helpers ────────────────────────────────

    /// <summary>ReferenceType cho FE deep-link — dùng tên BẢNG DB để đồng bộ với noti sẵn có.</summary>
    private const string EngagementReference = "project_provider";
    private const string ProjectReference = "project";

    /// <summary>Nạp engagement kèm tài khoản của cả hai bên (owner + provider).</summary>
    private Task<ProjectWorking?> LoadEngagementWithPartiesAsync(long projectWorkingId) =>
        _unitOfWork.GetRepository<ProjectWorking>().SingleOrDefaultAsync(
            predicate: e => e.Id == projectWorkingId,
            include: q => q
                .Include(e => e.ProjectShopOwner).ThenInclude(p => p.Owner).ThenInclude(o => o.Account)
                .Include(e => e.ServiceProviderProfile).ThenInclude(p => p.Account));

    /// <summary>Tạo bản ghi noti (lưu trước để làm lịch sử), rồi cố gắng gửi email (best-effort).</summary>
    private async Task CreateAndDispatchAsync(
        long accountId, string? email, string type, string title, string content,
        string referenceType, long referenceId)
    {
        var noti = new Notification
        {
            AccountId = accountId,
            Type = type,
            Title = title,
            Content = content,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(noti);
        await _unitOfWork.CommitAsync(); // lưu lịch sử trước — email lỗi vẫn còn bản ghi để resend

        if (string.IsNullOrWhiteSpace(email)) return;

        try
        {
            await SendEmailAsync(email, type, title, content);
            noti.EmailSentAt = DateTime.UtcNow;
            _repository.Update(noti);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            // KHÔNG throw — noti record đã lưu, có thể resend qua endpoint /resend.
            _logger.LogError(ex,
                "Gửi email noti '{Type}' tới {Email} thất bại (noti #{Id}). Có thể gửi lại sau.",
                type, email, noti.Id);
        }
    }

    private Task SendEmailAsync(string email, string type, string title, string content) =>
        _emailService.SendTemplateAsync(
            email,
            subject: NotificationTypes.SubjectFor(type),
            templateName: NotificationTypes.TemplateFor(type),
            placeholders: new Dictionary<string, string>
            {
                ["Title"] = title,
                ["Content"] = content,
                ["Year"] = DateTime.UtcNow.Year.ToString()
            });
}
