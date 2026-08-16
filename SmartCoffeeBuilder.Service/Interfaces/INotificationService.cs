using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Responses.Notification;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface INotificationService
{
    /// <summary>Lịch sử noti của một account (mới nhất trước), lọc theo trạng thái đã đọc.</summary>
    Task<PaginationResponse<NotificationResponse>> GetForAccountAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 20, bool? isRead = null);

    /// <summary>Chỉ noti của chính <paramref name="accountId"/>; của người khác trả 404.</summary>
    Task<NotificationResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>Số noti chưa đọc của account (badge cho FE/mobile).</summary>
    Task<int> GetUnreadCountAsync(Guid accountId);

    Task<NotificationResponse> MarkAsReadAsync(Guid accountId, Guid id);

    /// <summary>Đánh dấu đã đọc tất cả noti của account. Trả về số dòng cập nhật.</summary>
    Task<int> MarkAllAsReadAsync(Guid accountId);

    /// <summary>Gửi lại email cho một noti đã có (vd lần trước gửi lỗi). Cập nhật EmailSentAt.</summary>
    Task<NotificationResponse> ResendAsync(Guid accountId, Guid id);

    // ── Domain triggers (tạo bản ghi noti + gửi email). Best-effort: lỗi email KHÔNG throw. ──

    /// <summary>Owner nhận noti khi có provider ứng tuyển vào bài đăng của họ.</summary>
    Task NotifyApplicationReceivedAsync(Guid applicationId);

    /// <summary>Provider nhận noti khi hồ sơ được chấp nhận (accepted=true) hoặc từ chối (false).</summary>
    Task NotifyApplicationDecisionAsync(Guid applicationId, bool accepted);

    // ── Lời mời hợp tác trực tiếp (direct-hire) ──

    /// <summary>Provider nhận noti khi owner mời hợp tác trực tiếp (thuê thẳng).</summary>
    Task NotifyEngagementInvitedAsync(Guid projectWorkingId);

    /// <summary>
    /// Owner (người gửi lời mời) nhận noti khi provider phản hồi lời mời hợp tác trực tiếp:
    /// <paramref name="accepted"/> = true (nhận) hoặc false (từ chối).
    /// </summary>
    Task NotifyEngagementInviteDecisionAsync(Guid projectWorkingId, bool accepted);

    // ── Luồng đóng engagement / đóng dự án ──

    /// <summary>Owner nhận noti khi provider báo đã xong việc và xin nghiệm thu.</summary>
    Task NotifyEngagementCompletionRequestedAsync(Guid projectWorkingId);

    /// <summary>Provider nhận noti khi owner nghiệm thu engagement — kèm nhắc dự án đã mở khoá review.</summary>
    Task NotifyEngagementCompletedAsync(Guid projectWorkingId);

    /// <summary>
    /// Bên CÒN LẠI nhận noti khi engagement bị huỷ ngang KHÔNG qua đồng thuận (admin can thiệp).
    /// <paramref name="terminatedByOwner"/> = true khi owner là người huỷ (noti gửi provider), ngược lại gửi owner.
    /// Luồng huỷ ngang thông thường (hai bên đồng thuận) dùng bộ 3 method bên dưới.
    /// </summary>
    Task NotifyEngagementTerminatedAsync(Guid projectWorkingId, bool terminatedByOwner);

    // ── Huỷ ngang cần đồng thuận hai bên ──

    /// <summary>
    /// Bên CÒN LẠI nhận noti + email khi một bên vừa đề nghị huỷ ngang — cần họ đồng ý/từ chối.
    /// <paramref name="requestedByOwner"/> = true khi owner là bên đề nghị (noti gửi provider).
    /// </summary>
    Task NotifyEngagementTerminationRequestedAsync(Guid projectWorkingId, bool requestedByOwner);

    /// <summary>
    /// Bên ĐỀ NGHỊ nhận noti + email khi bên kia phản hồi đề nghị huỷ ngang.
    /// <paramref name="approved"/> = true (đồng ý, engagement đã 'terminated') hoặc false (từ chối, giữ 'accepted').
    /// </summary>
    Task NotifyEngagementTerminationDecisionAsync(Guid projectWorkingId, bool requestedByOwner, bool approved);

    /// <summary>Bên CÒN LẠI nhận noti + email khi bên đề nghị tự rút lại đề nghị huỷ ngang.</summary>
    Task NotifyEngagementTerminationCancelledAsync(Guid projectWorkingId, bool requestedByOwner);

    /// <summary>
    /// OWNER nhận noti nhắc đóng dự án khi engagement mở cuối cùng vừa khép lại và dự án đã đủ
    /// điều kiện nghiệm thu. Không đủ điều kiện thì không gửi gì — caller cứ gọi vô tư.
    /// </summary>
    Task NotifyProjectReadyToCloseAsync(Guid projectShopOwnerId);

    /// <summary>
    /// Các provider liên quan nhận noti khi owner đóng (cancelled=false) hoặc huỷ (true) dự án.
    /// Người nhận lấy từ <paramref name="affectedProjectWorkingIds"/> — caller biết chính xác
    /// engagement nào vừa bị ảnh hưởng nên không gửi nhầm provider đã bị từ chối từ lâu.
    /// </summary>
    Task NotifyProjectClosedAsync(
        Guid projectShopOwnerId, bool cancelled, IReadOnlyCollection<Guid> affectedProjectWorkingIds);
}
