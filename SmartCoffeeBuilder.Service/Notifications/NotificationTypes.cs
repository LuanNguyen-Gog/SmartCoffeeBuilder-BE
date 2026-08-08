namespace SmartCoffeeBuilder.Service.Notifications;

/// <summary>
/// Hằng loại thông báo — lưu vào cột notification.type và dùng để chọn template email.
/// Giữ dạng chuỗi (không enum) cho khớp cột text tự do, dễ mở rộng thêm loại mới.
/// </summary>
public static class NotificationTypes
{
    /// <summary>Gửi cho OWNER khi có provider ứng tuyển vào bài đăng của họ.</summary>
    public const string ApplicationReceived = "application_received";

    /// <summary>Gửi cho PROVIDER khi hồ sơ ứng tuyển được chấp nhận.</summary>
    public const string ApplicationAccepted = "application_accepted";

    /// <summary>Gửi cho PROVIDER khi hồ sơ ứng tuyển bị từ chối.</summary>
    public const string ApplicationRejected = "application_rejected";

    // ───────── Lời mời hợp tác trực tiếp (direct-hire) ─────────

    /// <summary>Gửi cho PROVIDER khi owner mời hợp tác trực tiếp (thuê thẳng, không qua bài đăng).</summary>
    public const string EngagementInvited = "engagement_invited";

    /// <summary>Gửi cho OWNER khi provider NHẬN lời mời hợp tác trực tiếp.</summary>
    public const string EngagementInviteAccepted = "engagement_invite_accepted";

    /// <summary>Gửi cho OWNER khi provider TỪ CHỐI lời mời hợp tác trực tiếp.</summary>
    public const string EngagementInviteRejected = "engagement_invite_rejected";

    // ───────── Luồng đóng engagement / đóng dự án ─────────

    /// <summary>Gửi cho OWNER khi provider báo đã xong việc và xin nghiệm thu.</summary>
    public const string EngagementCompletionRequested = "engagement_completion_requested";

    /// <summary>Gửi cho PROVIDER khi owner nghiệm thu engagement (mở khoá review).</summary>
    public const string EngagementCompleted = "engagement_completed";

    /// <summary>Gửi cho BÊN CÒN LẠI khi một bên huỷ ngang engagement đang chạy.</summary>
    public const string EngagementTerminated = "engagement_terminated";

    /// <summary>
    /// Gửi cho OWNER khi engagement mở cuối cùng của dự án vừa đóng lại và dự án đã đủ điều kiện
    /// nghiệm thu — nhắc owner bấm đóng dự án (POST /api/project-shop-owners/{id}/complete).
    /// </summary>
    public const string ProjectReadyToClose = "project_ready_to_close";

    /// <summary>Gửi cho các PROVIDER đã tham gia khi owner đóng dự án.</summary>
    public const string ProjectCompleted = "project_completed";

    /// <summary>Gửi cho các PROVIDER đang hợp tác khi owner huỷ dự án.</summary>
    public const string ProjectCancelled = "project_cancelled";

    /// <summary>Tên file template email (không đuôi .html) tương ứng mỗi loại.</summary>
    public static string TemplateFor(string type) => type switch
    {
        ApplicationReceived => "ApplicationReceivedEmail",
        ApplicationAccepted => "ApplicationAcceptedEmail",
        ApplicationRejected => "ApplicationRejectedEmail",
        // Nhóm đóng engagement/dự án dùng template chung — nội dung đã đủ rõ trong Title/Content.
        _ => "NotificationEmail" // template chung dự phòng
    };

    /// <summary>Subject email mặc định cho mỗi loại.</summary>
    public static string SubjectFor(string type) => type switch
    {
        ApplicationReceived => "Hồ sơ ứng tuyển mới - Smart Coffee Builder",
        ApplicationAccepted => "Hồ sơ của bạn đã được chấp nhận - Smart Coffee Builder",
        ApplicationRejected => "Kết quả hồ sơ ứng tuyển - Smart Coffee Builder",
        EngagementInvited => "Bạn nhận được lời mời hợp tác - Smart Coffee Builder",
        EngagementInviteAccepted => "Nhà cung cấp đã nhận lời mời hợp tác - Smart Coffee Builder",
        EngagementInviteRejected => "Nhà cung cấp đã từ chối lời mời hợp tác - Smart Coffee Builder",
        EngagementCompletionRequested => "Nhà cung cấp báo hoàn thành, chờ bạn nghiệm thu - Smart Coffee Builder",
        EngagementCompleted => "Công việc của bạn đã được nghiệm thu - Smart Coffee Builder",
        EngagementTerminated => "Hợp tác đã bị huỷ ngang - Smart Coffee Builder",
        ProjectReadyToClose => "Dự án đã xong, chờ bạn đóng - Smart Coffee Builder",
        ProjectCompleted => "Dự án đã hoàn thành - Smart Coffee Builder",
        ProjectCancelled => "Dự án đã bị huỷ - Smart Coffee Builder",
        _ => "Thông báo - Smart Coffee Builder"
    };
}
