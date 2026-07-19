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
}
