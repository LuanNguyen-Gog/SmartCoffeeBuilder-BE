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
            ?? throw new KeyNotFoundException($"No notification found with id {id}.");

        var email = noti.Account?.Email;
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("The notification's account has no email to resend to.");

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
        ?? throw new KeyNotFoundException($"No notification found with id {id}.");

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
            _logger.LogWarning("Skipping application_received notification: could not resolve the owner for application #{Id}.", applicationId);
            return;
        }

        var providerName = app.ServiceProviderProfile?.DisplayName ?? "A provider";
        var projectName = app.Post?.ProjectShopOwner?.Name ?? "your project";
        var postTitle = app.Post?.Title ?? "the post";

        var title = "New application for your project";
        var content = $"Provider \"{providerName}\" has applied to post \"{postTitle}\" " +
                      $"in project \"{projectName}\". Please review and respond to the application.";

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
            _logger.LogWarning("Skipping application decision notification: could not resolve the provider for application #{Id}.", applicationId);
            return;
        }

        var projectName = app.Post?.ProjectShopOwner?.Name ?? "the project";
        var postTitle = app.Post?.Title ?? "the post";

        string type, title, content;
        if (accepted)
        {
            type = NotificationTypes.ApplicationAccepted;
            title = "Your application has been accepted";
            content = $"Congratulations! Your application for post \"{postTitle}\" (project \"{projectName}\") " +
                      "has been accepted by the shop owner. Both sides can now start the work.";
        }
        else
        {
            type = NotificationTypes.ApplicationRejected;
            title = "Your application was not selected";
            content = $"Unfortunately, your application for post \"{postTitle}\" (project \"{projectName}\") " +
                      "was not selected this time. Thank you for your interest.";
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
                "Skipping engagement_invited notification: could not resolve the provider for engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var shopName = engagement.ProjectShopOwner?.Owner?.ShopName;
        var ownerLabel = string.IsNullOrWhiteSpace(shopName) ? "A shop owner" : $"\"{shopName}\"";
        var projectName = engagement.ProjectShopOwner?.Name ?? "a project";

        var note = string.IsNullOrWhiteSpace(engagement.RequestMessage)
            ? string.Empty
            : $" Message: \"{engagement.RequestMessage}\".";

        await CreateAndDispatchAsync(
            providerAccount.Id, providerAccount.Email, NotificationTypes.EngagementInvited,
            title: "You have received a direct engagement invitation",
            content: $"{ownerLabel} has invited you to work together ({engagement.ContractType}) on project \"{projectName}\".{note} " +
                     "Please respond to the invitation (accept or decline).",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementInviteDecisionAsync(Guid projectWorkingId, bool accepted)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        var ownerAccount = engagement?.ProjectShopOwner?.Owner?.Account;
        if (engagement is null || ownerAccount is null)
        {
            _logger.LogWarning(
                "Skipping engagement_invite decision notification: could not resolve the owner for engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var providerName = engagement.ServiceProviderProfile?.DisplayName ?? "The provider";
        var projectName = engagement.ProjectShopOwner?.Name ?? "your project";

        string type, title, content;
        if (accepted)
        {
            type = NotificationTypes.EngagementInviteAccepted;
            title = "The provider accepted your engagement invitation";
            content = $"\"{providerName}\" accepted the engagement invitation ({engagement.ContractType}) " +
                      $"for project \"{projectName}\". Both sides can now begin (sign the contract, run the survey...).";
        }
        else
        {
            type = NotificationTypes.EngagementInviteRejected;
            title = "The provider declined your engagement invitation";
            content = $"\"{providerName}\" declined the engagement invitation ({engagement.ContractType}) " +
                      $"for project \"{projectName}\". You can invite another provider.";
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
                "Skipping engagement_completion_requested notification: could not resolve the owner for engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var providerName = engagement.ServiceProviderProfile?.DisplayName ?? "The provider";
        var projectName = engagement.ProjectShopOwner?.Name ?? "your project";

        var note = string.IsNullOrWhiteSpace(engagement.CompletionRequestNote)
            ? string.Empty
            : $" Handover note: \"{engagement.CompletionRequestNote}\".";

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, NotificationTypes.EngagementCompletionRequested,
            title: "The provider reported completion and is awaiting your acceptance",
            content: $"\"{providerName}\" has reported completing their part of the work ({engagement.ContractType}) " +
                     $"in project \"{projectName}\".{note} Please review it and click accept to finalise the engagement.",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementCompletedAsync(Guid projectWorkingId)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        var providerAccount = engagement?.ServiceProviderProfile?.Account;
        if (engagement is null || providerAccount is null)
        {
            _logger.LogWarning(
                "Skipping engagement_completed notification: could not resolve the provider for engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var projectName = engagement.ProjectShopOwner?.Name ?? "the project";

        await CreateAndDispatchAsync(
            providerAccount.Id, providerAccount.Email, NotificationTypes.EngagementCompleted,
            title: "Your work has been accepted",
            content: $"The shop owner has accepted your part of the work ({engagement.ContractType}) in project " +
                     $"\"{projectName}\". The engagement is complete — the shop owner can now leave you a review.",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementTerminatedAsync(Guid projectWorkingId, bool terminatedByOwner)
    {
        var engagement = await LoadEngagementWithPartiesAsync(projectWorkingId);
        if (engagement is null)
        {
            _logger.LogWarning("Skipping engagement_terminated notification: engagement #{Id} was not found.", projectWorkingId);
            return;
        }

        // Người huỷ đã biết rồi — chỉ báo cho bên còn lại.
        var recipient = terminatedByOwner
            ? engagement.ServiceProviderProfile?.Account
            : engagement.ProjectShopOwner?.Owner?.Account;
        if (recipient is null)
        {
            _logger.LogWarning(
                "Skipping engagement_terminated notification: could not resolve the recipient for engagement #{Id}.",
                projectWorkingId);
            return;
        }

        var projectName = engagement.ProjectShopOwner?.Name ?? "the project";
        var actor = terminatedByOwner ? "The shop owner" : "The provider";

        await CreateAndDispatchAsync(
            recipient.Id, recipient.Email, NotificationTypes.EngagementTerminated,
            title: "The engagement was terminated early",
            content: $"{actor} terminated the engagement ({engagement.ContractType}) in project \"{projectName}\". " +
                     "All related work in this engagement stops as of now.",
            referenceType: EngagementReference, referenceId: engagement.Id);
    }

    public async Task NotifyEngagementTerminationRequestedAsync(Guid projectWorkingId, bool requestedByOwner)
    {
        var ctx = await LoadTerminationContextAsync(
            projectWorkingId, requestedByOwner, toRequester: false, logFor: "engagement_termination_requested");
        if (ctx is null) return;

        var note = string.IsNullOrWhiteSpace(ctx.Engagement.TerminationRequestNote)
            ? string.Empty
            : $" Reason: \"{ctx.Engagement.TerminationRequestNote}\".";

        await CreateAndDispatchAsync(
            ctx.Recipient.Id, ctx.Recipient.Email, NotificationTypes.EngagementTerminationRequested,
            title: "Early termination requested, awaiting your response",
            content: $"{ctx.RequesterLabel} has requested to terminate the engagement ({ctx.ContractType}) early in project " +
                     $"\"{ctx.ProjectName}\".{note} The engagement IS STILL running until you agree — " +
                     "please open the engagement to accept or decline this request.",
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
               "The engagement ended by mutual agreement",
               $"{ctx.CounterpartLabel} accepted your early termination request. The engagement ({ctx.ContractType}) " +
               $"in project \"{ctx.ProjectName}\" ends as of now. Both sides can start again " +
               "with a new engagement invitation or through a recruitment post.")
            : (NotificationTypes.EngagementTerminationRejected,
               "The early termination request was declined",
               $"{ctx.CounterpartLabel} did not agree to terminate the engagement ({ctx.ContractType}) early in project " +
               $"\"{ctx.ProjectName}\". The engagement continues — the two sides should talk it over and reach an agreement.");

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
            title: "The early termination request was withdrawn",
            content: $"{ctx.RequesterLabel} has withdrawn the request to terminate the engagement ({ctx.ContractType}) early in project " +
                     $"\"{ctx.ProjectName}\". You no longer need to respond; the engagement continues as normal.",
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
                "Skipping project_ready_to_close notification: could not resolve the owner for project #{Id}.",
                projectShopOwnerId);
            return;
        }

        // KHÔNG chặn theo "đã có noti chưa đọc": owner có thể mời thêm provider sau khi được nhắc,
        // lúc đó lời nhắc cũ thành sai và phải có lời nhắc mới khi hợp tác mới khép lại. Mỗi lần
        // gửi ứng với đúng một lần dự án chuyển sang trạng thái đóng được, nên không sinh trùng.
        var plural = completedCount > 1 ? $"all {completedCount} engagements" : "the engagement";

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, NotificationTypes.ProjectReadyToClose,
            title: "The project is finished and waiting for you to close it",
            content: $"Project \"{project.Name}\" has finished acceptance for {plural} and no engagement is " +
                     "still running. Open the project and click \"Complete project\" to close and finish it.",
            referenceType: ProjectReference, referenceId: projectShopOwnerId);
    }

    public async Task NotifyProjectClosedAsync(
        Guid projectShopOwnerId, bool cancelled, IReadOnlyCollection<Guid> affectedProjectWorkingIds)
    {
        if (affectedProjectWorkingIds.Count == 0) return;

        var project = await _unitOfWork.GetRepository<ProjectShopOwner>()
            .SingleOrDefaultAsync(predicate: p => p.Id == projectShopOwnerId);
        var projectName = project?.Name ?? "the project";

        var engagements = await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
            predicate: e => affectedProjectWorkingIds.Contains(e.Id),
            include: q => q.Include(e => e.ServiceProviderProfile).ThenInclude(p => p.Account));

        var type = cancelled ? NotificationTypes.ProjectCancelled : NotificationTypes.ProjectCompleted;
        var title = cancelled ? "The project was cancelled" : "The project is complete";

        // Một provider có thể có nhiều engagement trong cùng dự án — chỉ gửi 1 noti cho mỗi tài khoản.
        var recipients = engagements
            .Select(e => e.ServiceProviderProfile?.Account)
            .Where(a => a != null)
            .GroupBy(a => a!.Id)
            .Select(g => g.First()!);

        foreach (var account in recipients)
        {
            var content = cancelled
                ? $"The shop owner cancelled project \"{projectName}\". Your open engagements in this project have been closed."
                : $"Project \"{projectName}\" has been closed and completed by the shop owner. Thank you for working with us.";

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
                "Skipping construction_overdue notification: could not resolve the owner for construction item #{Id}.",
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
        var providerName = item.ProjectWorking?.ServiceProviderProfile?.DisplayName ?? "The provider";
        var projectName = item.ProjectWorking?.ProjectShopOwner?.Name ?? "your project";

        await CreateAndDispatchAsync(
            ownerAccount.Id, ownerAccount.Email, NotificationTypes.ConstructionOverdue,
            title: $"Construction item \"{item.Name}\" is behind schedule",
            content: $"Construction item \"{item.Name}\" in project \"{projectName}\" was due on " +
                     $"{due:dd/MM/yyyy} but is still not finished ({daysLate} day(s) late). " +
                     $"Provider in charge: \"{providerName}\". " +
                     "You should talk to the provider directly about the schedule; the system does not hold " +
                     "funds and makes no automatic deductions — any payment adjustment is agreed between the two parties.",
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
            _logger.LogWarning("Skipping {Type} notification: engagement #{Id} was not found.", logFor, projectWorkingId);
            return null;
        }

        var ownerAccount = engagement.ProjectShopOwner?.Owner?.Account;
        var providerAccount = engagement.ServiceProviderProfile?.Account;

        // requestedByOwner quyết định ai là "bên đề nghị"; toRequester quyết định gửi về phía nào.
        var recipient = (requestedByOwner == toRequester) ? ownerAccount : providerAccount;
        if (recipient is null)
        {
            _logger.LogWarning(
                "Skipping {Type} notification: could not resolve the recipient for engagement #{Id}.", logFor, projectWorkingId);
            return null;
        }

        var ownerLabel = "The shop owner";
        var providerLabel = engagement.ServiceProviderProfile?.DisplayName is { Length: > 0 } name
            ? $"Provider \"{name}\""
            : "The provider";

        return new TerminationNotificationContext(
            engagement, recipient,
            RequesterLabel: requestedByOwner ? ownerLabel : providerLabel,
            CounterpartLabel: requestedByOwner ? providerLabel : ownerLabel,
            ProjectName: engagement.ProjectShopOwner?.Name ?? "the project",
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
                "Failed to send '{Type}' notification email to {Email} (notification #{Id}). It can be resent later.",
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
