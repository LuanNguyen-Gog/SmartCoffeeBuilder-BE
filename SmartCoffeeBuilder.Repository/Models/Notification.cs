namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Thông báo trong ứng dụng — vừa là lịch sử noti cho FE/mobile, vừa là bản ghi
/// để gửi/gửi lại email. Một noti có thể đã gửi email (EmailSentAt != null) hoặc chưa.
/// </summary>
public class Notification
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    /// <summary>Loại noti (xem NotificationTypes) — dùng để chọn template email + hiển thị icon ở FE.</summary>
    public string Type { get; set; } = null!;
    /// <summary>Tiêu đề ngắn — dùng cho subject email và tiêu đề trong danh sách noti.</summary>
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    /// <summary>Loại tài nguyên liên quan (vd "project_application") — cho FE deep-link.</summary>
    public string? ReferenceType { get; set; }
    /// <summary>Id tài nguyên liên quan (vd application id).</summary>
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    /// <summary>Lần cuối gửi email thành công; null = chưa gửi được (có thể resend).</summary>
    public DateTime? EmailSentAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
}
