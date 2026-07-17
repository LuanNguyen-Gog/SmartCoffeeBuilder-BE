using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
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
        var app = await _unitOfWork.GetRepository<ProjectApplication>().SingleOrDefaultAsync(
            predicate: a => a.Id == applicationId,
            include: q => q
                .Include(a => a.Post).ThenInclude(p => p.Project).ThenInclude(pr => pr.Owner).ThenInclude(o => o.Account)
                .Include(a => a.Provider));

        var ownerAccount = app?.Post?.Project?.Owner?.Account;
        if (app is null || ownerAccount is null)
        {
            _logger.LogWarning("Bỏ qua noti application_received: không resolve được owner cho application #{Id}.", applicationId);
            return;
        }

        var providerName = app.Provider?.DisplayName ?? "Một nhà cung cấp";
        var projectName = app.Post?.Project?.Name ?? "dự án của bạn";
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
        var app = await _unitOfWork.GetRepository<ProjectApplication>().SingleOrDefaultAsync(
            predicate: a => a.Id == applicationId,
            include: q => q
                .Include(a => a.Post).ThenInclude(p => p.Project)
                .Include(a => a.Provider).ThenInclude(pr => pr.Account));

        var providerAccount = app?.Provider?.Account;
        if (app is null || providerAccount is null)
        {
            _logger.LogWarning("Bỏ qua noti application decision: không resolve được provider cho application #{Id}.", applicationId);
            return;
        }

        var projectName = app.Post?.Project?.Name ?? "dự án";
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

    // ──────────────────────────────── Helpers ────────────────────────────────

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
