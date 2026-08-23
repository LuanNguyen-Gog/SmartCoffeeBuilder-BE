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
using SmartCoffeeBuilder.Service.Utils;

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
        Guid accountId, int pageNumber = 1, int pageSize = 20, bool? isRead = null)
    {
        var query = _repository
            .GetQueryable(n => n.AccountId == accountId && (isRead == null || n.IsRead == isRead))
            .OrderByDescending(n => n.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<NotificationResponse>(
            paged.Items.Select(NotificationResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<NotificationResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var noti = await LoadOwnNotificationAsync(accountId, id);
        return NotificationResponse.From(noti);
    }

    public Task<int> GetUnreadCountAsync(Guid accountId) =>
        _repository.CountAsync(n => n.AccountId == accountId && !n.IsRead);

    public async Task<NotificationResponse> MarkAsReadAsync(Guid accountId, Guid id)
    {
        var noti = await LoadOwnNotificationAsync(accountId, id);

        if (!noti.IsRead)
        {
            noti.IsRead = true;
            _repository.Update(noti);
            await _unitOfWork.CommitAsync();
        }

        return NotificationResponse.From(noti);
    }

    public async Task<int> MarkAllAsReadAsync(Guid accountId)
    {
        var unread = await _repository.GetListAsync(
            predicate: n => n.AccountId == accountId && !n.IsRead);

        if (unread.Count == 0) return 0;

        foreach (var noti in unread) noti.IsRead = true;
        _repository.UpdateRange(unread);
        await _unitOfWork.CommitAsync();

        return unread.Count;
    }

    public async Task<NotificationResponse> ResendAsync(Guid accountId, Guid id)
    {
        // Gửi lại email = phát tán nội dung noti tới hộp thư của chủ noti — chỉ chính chủ được gọi.
        var noti = await _repository.SingleOrDefaultAsync(
            predicate: n => n.Id == id && n.AccountId == accountId,
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

    /// <summary>
    /// Nạp noti CỦA CHÍNH tài khoản đang đăng nhập. Noti của người khác trả 404 chứ không phải 401:
    /// không tiết lộ rằng id đó có tồn tại.
    /// </summary>
    private async Task<Notification> LoadOwnNotificationAsync(Guid accountId, Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: n => n.Id == id && n.AccountId == accountId)
        ?? throw new KeyNotFoundException($"Không tìm thấy notification với id {id}.");

    // ──────────────────────────────── Domain triggers ────────────────────────────────

    public async Task NotifyApplicationReceivedAsync(Guid applicationId)
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

    public async Task NotifyApplicationDecisionAsync(Guid applicationId, bool accepted)
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

    public async Task NotifyEngagementInvitedAsync(Guid projectWorkingId)
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

    public async Task NotifyEngagementInviteDecisionAsync(Guid projectWorkingId, bool accepted)
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

    public async Task NotifyEngagementCompletionRequestedAsync(Guid projectWorkingId)
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

    public async Task NotifyEngagementCompletedAsync(Guid projectWorkingId)
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

    public async Task NotifyEngagementTerminatedAsync(Guid projectWorkingId, bool terminatedByOwner)
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

    public async Task NotifyEngagementTerminationRequestedAsync(Guid projectWorkingId, bool requestedByOwner)
    {
        var ctx = await LoadTerminationContextAsync(
            projectWorkingId, requestedByOwner, toRequester: false, logFor: "engagement_termination_requested");
        if (ctx is null) return;

        var note = string.IsNullOrWhiteSpace(ctx.Engagement.TerminationRequestNote)
            ? string.Empty
            : $" Lý do: \"{ctx.Engagement.TerminationRequestNote}\".";

        await CreateAndDispatchAsync(
            ctx.Recipient.Id, ctx.Recipient.Email, NotificationTypes.EngagementTerminationRequested,
            title: "Đề nghị huỷ ngang hợp tác, chờ bạn phản hồi",
            content: $"{ctx.RequesterLabel} đề nghị huỷ ngang hợp tác ({ctx.ContractType}) tại dự án " +
                     $"\"{ctx.ProjectName}\".{note} Hợp tác VẪN đang chạy cho tới khi bạn đồng ý — " +
                     "vui lòng vào hợp tác để đồng ý hoặc từ chối đề nghị này.",
            referenceType: EngagementReference, referenceId: projectWorkingId);
    }

    public async Task NotifyEngagementTerminationDecisionAsync(
        Guid projectWorkingId, bool requestedByOwner, bool approved)
    {
        var ctx = await LoadTerminationContextAsync(
            projectWorkingId, requestedByOwner, toRequester: true, logFor: "engagement_termination decision");
        if (ctx is null) return;

        var (type, title, content) = approved
            ? (NotificationTypes.EngagementTerminationApproved,
               "Hợp tác đã kết thúc theo thoả thuận hai bên",
               $"{ctx.CounterpartLabel} đã đồng ý đề nghị huỷ ngang của bạn. Hợp tác ({ctx.ContractType}) " +
               $"tại dự án \"{ctx.ProjectName}\" kết thúc từ thời điểm hiện tại. Hai bên có thể bắt đầu lại " +
               "bằng một lời mời hợp tác mới hoặc qua bài đăng tuyển.")
            : (NotificationTypes.EngagementTerminationRejected,
               "Đề nghị huỷ ngang không được chấp thuận",
               $"{ctx.CounterpartLabel} không đồng ý huỷ ngang hợp tác ({ctx.ContractType}) tại dự án " +
               $"\"{ctx.ProjectName}\". Hợp tác vẫn tiếp tục — hai bên nên trao đổi lại để thống nhất.");

        await CreateAndDispatchAsync(
            ctx.Recipient.Id, ctx.Recipient.Email, type, title, content,
            referenceType: EngagementReference, referenceId: projectWorkingId);
    }

    public async Task NotifyEngagementTerminationCancelledAsync(Guid projectWorkingId, bool requestedByOwner)
    {
        var ctx = await LoadTerminationContextAsync(
            projectWorkingId, requestedByOwner, toRequester: false, logFor: "engagement_termination_cancelled");
        if (ctx is null) return;

        await CreateAndDispatchAsync(
            ctx.Recipient.Id, ctx.Recipient.Email, NotificationTypes.EngagementTerminationCancelled,
            title: "Đề nghị huỷ ngang đã được rút lại",
            content: $"{ctx.RequesterLabel} đã rút lại đề nghị huỷ ngang hợp tác ({ctx.ContractType}) tại dự án " +
                     $"\"{ctx.ProjectName}\". Bạn không cần phản hồi nữa, hợp tác tiếp tục như bình thường.",
            referenceType: EngagementReference, referenceId: projectWorkingId);
    }

    public async Task NotifyProjectReadyToCloseAsync(Guid projectShopOwnerId)
    {
        var project = await _unitOfWork.GetRepository<ProjectShopOwner>().SingleOrDefaultAsync(
            predicate: p => p.Id == projectShopOwnerId && p.DeletedAt == null,
            include: q => q.Include(p => p.Owner).ThenInclude(o => o.Account)
                           .Include(p => p.ProjectWorkings));
        if (project is null) return;

        // Dự án đã đóng/huỷ rồi thì không còn gì để nhắc.
        if (project.Status is not (ProjectStatus.briefed or ProjectStatus.in_progress)) return;

        // ĐÚNG luật mà ProjectShopOwnerService.CompleteAsync dùng để chặn — cả hai gọi chung
        // ProjectClosureRules để không bao giờ mời owner đóng một dự án mà guard sẽ từ chối.
        // Chưa đủ điều kiện thì im lặng: caller cứ gọi vô tư.
        var signedEngagementIds = (await _unitOfWork.GetRepository<Contract>().GetListAsync(
            selector: c => c.ProjectWorkingId,
            predicate: c => c.ProjectWorking.ProjectShopOwnerId == projectShopOwnerId
                            && c.Status == ContractStatus.confirmed)).ToHashSet();

        if (ProjectClosureRules.FindBlocker(project.ProjectWorkings, signedEngagementIds) != null) return;

        var completedCount = project.ProjectWorkings.Count(e => e.Status == ProviderStatus.completed);

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
        Guid projectShopOwnerId, bool cancelled, IReadOnlyCollection<Guid> affectedProjectWorkingIds)
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

    public async Task<bool> NotifyConstructionOverdueAsync(Guid constructionItemId, int renotifyAfterDays = 7)
    {
        var item = await _unitOfWork.GetRepository<ConstructionItem>().SingleOrDefaultAsync(
            predicate: ci => ci.Id == constructionItemId,
            include: q => q
                .Include(ci => ci.ProjectWorking).ThenInclude(e => e.ProjectShopOwner).ThenInclude(p => p.Owner).ThenInclude(o => o.Account)
                .Include(ci => ci.ProjectWorking).ThenInclude(e => e.ServiceProviderProfile));

        var ownerAccount = item?.ProjectWorking?.ProjectShopOwner?.Owner?.Account;
        if (item is null || ownerAccount is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti construction_overdue: không resolve được owner cho hạng mục #{Id}.",
                constructionItemId);
            return false;
        }

        if (item.EstimateAt is not DateOnly due) return false;

        // Đã báo trong cửa sổ renotify thì im — job chạy hằng ngày, không có chốt này thì owner
        // nhận đúng một tin mỗi sáng cho tới khi hạng mục xong.
        var since = DateTime.UtcNow.AddDays(-renotifyAfterDays);
        var recentlyNotified = await _repository.CountAsync(
            n => n.AccountId == ownerAccount.Id
                 && n.Type == NotificationTypes.ConstructionOverdue
                 && n.ReferenceId == item.Id
                 && n.CreatedAt >= since);
        if (recentlyNotified > 0) return false;

        // Số ngày trễ đếm theo giờ VN cho khớp con số owner tự nhẩm trên lịch của họ.
        var daysLate = VietnamTime.Today.DayNumber - due.DayNumber;
        var providerName = item.ProjectWorking?.ServiceProviderProfile?.DisplayName ?? "Nhà cung cấp";
        var projectName = item.ProjectWorking?.ProjectShopOwner?.Name ?? "dự án của bạn";

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, NotificationTypes.ConstructionOverdue,
            title: $"Hạng mục \"{item.Name}\" đang trễ tiến độ",
            content: $"Hạng mục \"{item.Name}\" thuộc dự án \"{projectName}\" có hạn hoàn thành " +
                     $"{due:dd/MM/yyyy} nhưng đến nay vẫn chưa xong (trễ {daysLate} ngày). " +
                     $"Nhà cung cấp phụ trách: \"{providerName}\". " +
                     "Bạn nên trao đổi trực tiếp với nhà cung cấp về tiến độ; hệ thống không giữ " +
                     "tiền và không tự khấu trừ, mọi điều chỉnh thanh toán do hai bên tự thoả thuận.",
            referenceType: ConstructionItemReference, referenceId: item.Id);

        return true;
    }

    // ──────────────────────────────── Helpers ────────────────────────────────

    /// <summary>ReferenceType cho FE deep-link — dùng tên BẢNG DB để đồng bộ với noti sẵn có.</summary>
    private const string EngagementReference = "project_provider";
    private const string ProjectReference = "project";
    private const string ConstructionItemReference = "construction_item";

    /// <summary>
    /// Dữ liệu chung của 3 noti huỷ-ngang-đồng-thuận: engagement, người nhận, và nhãn hiển thị
    /// của hai bên. <c>RequesterLabel</c> luôn là bên gửi đề nghị, <c>CounterpartLabel</c> là bên phản hồi.
    /// </summary>
    private sealed record TerminationNotificationContext(
        ProjectWorking Engagement, Account Recipient,
        string RequesterLabel, string CounterpartLabel, string ProjectName, string ContractType);

    /// <summary>
    /// Nạp engagement + chọn người nhận cho một noti huỷ ngang.
    /// <paramref name="toRequester"/> = true gửi cho bên ĐỀ NGHỊ, false gửi cho bên CÒN LẠI.
    /// Trả null (kèm log) khi không resolve được — noti là best-effort, không chặn nghiệp vụ.
    /// </summary>
    private async Task<TerminationNotificationContext?> LoadTerminationContextAsync(
        Guid projectWorkingId, bool requestedByOwner, bool toRequester, string logFor)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        if (engagement is null)
        {
            _logger.LogWarning("Bỏ qua noti {Type}: không tìm thấy engagement #{Id}.", logFor, projectWorkingId);
            return null;
        }

        var ownerAccount = engagement.ProjectShopOwner?.Owner?.Account;
        var providerAccount = engagement.ServiceProviderProfile?.Account;

        // requestedByOwner quyết định ai là "bên đề nghị"; toRequester quyết định gửi về phía nào.
        var recipient = (requestedByOwner == toRequester) ? ownerAccount : providerAccount;
        if (recipient is null)
        {
            _logger.LogWarning(
                "Bỏ qua noti {Type}: không resolve được người nhận cho engagement #{Id}.", logFor, projectWorkingId);
            return null;
        }

        var ownerLabel = "Chủ quán";
        var providerLabel = engagement.ServiceProviderProfile?.DisplayName is { Length: > 0 } name
            ? $"Nhà cung cấp \"{name}\""
            : "Nhà cung cấp";

        return new TerminationNotificationContext(
            engagement, recipient,
            RequesterLabel: requestedByOwner ? ownerLabel : providerLabel,
            CounterpartLabel: requestedByOwner ? providerLabel : ownerLabel,
            ProjectName: engagement.ProjectShopOwner?.Name ?? "dự án",
            ContractType: engagement.ContractType.ToString());
    }

    /// <summary>Nạp engagement kèm tài khoản của cả hai bên (owner + provider).</summary>
    private Task<ProjectWorking?> LoadEngagementWithPartiesAsync(Guid projectWorkingId) =>
        _unitOfWork.GetRepository<ProjectWorking>().SingleOrDefaultAsync(
            predicate: e => e.Id == projectWorkingId,
            include: q => q
                .Include(e => e.ProjectShopOwner).ThenInclude(p => p.Owner).ThenInclude(o => o.Account)
                .Include(e => e.ServiceProviderProfile).ThenInclude(p => p.Account));

    /// <summary>Tạo bản ghi noti (lưu trước để làm lịch sử), rồi cố gắng gửi email (best-effort).</summary>
    private async Task CreateAndDispatchAsync(
        Guid accountId, string? email, string type, string title, string content,
        string referenceType, Guid referenceId)
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
