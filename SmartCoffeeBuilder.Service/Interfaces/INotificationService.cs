using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Responses.Notification;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface INotificationService
{
    /// <summary>Lịch sử noti của một account (mới nhất trước), lọc theo trạng thái đã đọc.</summary>
    Task<PaginationResponse<NotificationResponse>> GetForAccountAsync(
        long accountId, int pageNumber = 1, int pageSize = 20, bool? isRead = null);

    Task<NotificationResponse> GetByIdAsync(long id);

    /// <summary>Số noti chưa đọc của account (badge cho FE/mobile).</summary>
    Task<int> GetUnreadCountAsync(long accountId);

    Task<NotificationResponse> MarkAsReadAsync(long id);

    /// <summary>Đánh dấu đã đọc tất cả noti của account. Trả về số dòng cập nhật.</summary>
    Task<int> MarkAllAsReadAsync(long accountId);

    /// <summary>Gửi lại email cho một noti đã có (vd lần trước gửi lỗi). Cập nhật EmailSentAt.</summary>
    Task<NotificationResponse> ResendAsync(long id);

    // ── Domain triggers (tạo bản ghi noti + gửi email). Best-effort: lỗi email KHÔNG throw. ──

    /// <summary>Owner nhận noti khi có provider ứng tuyển vào bài đăng của họ.</summary>
    Task NotifyApplicationReceivedAsync(long applicationId);

    /// <summary>Provider nhận noti khi hồ sơ được chấp nhận (accepted=true) hoặc từ chối (false).</summary>
    Task NotifyApplicationDecisionAsync(long applicationId, bool accepted);

    // ── Luồng đóng engagement / đóng dự án ──

    /// <summary>Owner nhận noti khi provider báo đã xong việc và xin nghiệm thu.</summary>
    Task NotifyEngagementCompletionRequestedAsync(long projectWorkingId);

    /// <summary>Provider nhận noti khi owner nghiệm thu engagement — kèm nhắc dự án đã mở khoá review.</summary>
    Task NotifyEngagementCompletedAsync(long projectWorkingId);

    /// <summary>
    /// Bên CÒN LẠI nhận noti khi engagement bị huỷ ngang.
    /// <paramref name="terminatedByOwner"/> = true khi owner là người huỷ (noti gửi provider), ngược lại gửi owner.
    /// </summary>
    Task NotifyEngagementTerminatedAsync(long projectWorkingId, bool terminatedByOwner);

    /// <summary>
    /// Các provider liên quan nhận noti khi owner đóng (cancelled=false) hoặc huỷ (true) dự án.
    /// Người nhận lấy từ <paramref name="affectedProjectWorkingIds"/> — caller biết chính xác
    /// engagement nào vừa bị ảnh hưởng nên không gửi nhầm provider đã bị từ chối từ lâu.
    /// </summary>
    Task NotifyProjectClosedAsync(
        long projectShopOwnerId, bool cancelled, IReadOnlyCollection<long> affectedProjectWorkingIds);
}
