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

    // ───────── Huỷ ngang cần đồng thuận hai bên ─────────

    /// <summary>Gửi cho BÊN CÒN LẠI khi một bên đề nghị huỷ ngang — cần bên kia đồng ý.</summary>
    public const string EngagementTerminationRequested = "engagement_termination_requested";

    /// <summary>Gửi cho BÊN ĐỀ NGHỊ khi bên kia ĐỒNG Ý huỷ ngang (engagement chuyển 'terminated').</summary>
    public const string EngagementTerminationApproved = "engagement_termination_approved";

    /// <summary>Gửi cho BÊN ĐỀ NGHỊ khi bên kia TỪ CHỐI huỷ ngang (engagement giữ 'accepted').</summary>
    public const string EngagementTerminationRejected = "engagement_termination_rejected";

    /// <summary>Gửi cho BÊN CÒN LẠI khi bên đề nghị tự rút lại đề nghị huỷ ngang.</summary>
    public const string EngagementTerminationCancelled = "engagement_termination_cancelled";

    /// <summary>
    /// Gửi cho OWNER khi engagement mở cuối cùng của dự án vừa đóng lại và dự án đã đủ điều kiện
    /// nghiệm thu — nhắc owner bấm đóng dự án (POST /api/project-shop-owners/{id}/complete).
    /// </summary>
    public const string ProjectReadyToClose = "project_ready_to_close";

    /// <summary>Gửi cho các PROVIDER đã tham gia khi owner đóng dự án.</summary>
    public const string ProjectCompleted = "project_completed";

    /// <summary>Gửi cho các PROVIDER đang hợp tác khi owner huỷ dự án.</summary>
    public const string ProjectCancelled = "project_cancelled";

    // ───────── Cảnh báo tiến độ thi công ─────────

    /// <summary>
    /// Gửi cho OWNER khi một hạng mục thi công quá hạn hoàn thành mà chưa xong (review 3).
    /// Hệ thống chỉ BÁO — không giữ tiền, không tự phạt: owner tự làm việc với nhà cung cấp
    /// và tự trừ tiền ngoài nền tảng.
    /// </summary>
    public const string ConstructionOverdue = "construction_overdue";

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
        ApplicationReceived => "New application - Smart Coffee Builder",
        ApplicationAccepted => "Your application has been accepted - Smart Coffee Builder",
        ApplicationRejected => "Application result - Smart Coffee Builder",
        EngagementInvited => "You have received an engagement invitation - Smart Coffee Builder",
        EngagementInviteAccepted => "The provider accepted your engagement invitation - Smart Coffee Builder",
        EngagementInviteRejected => "The provider declined your engagement invitation - Smart Coffee Builder",
        EngagementCompletionRequested => "The provider reported completion, awaiting your acceptance - Smart Coffee Builder",
        EngagementCompleted => "Your work has been accepted - Smart Coffee Builder",
        EngagementTerminated => "The engagement was terminated early - Smart Coffee Builder",
        EngagementTerminationRequested => "Early termination requested, awaiting your response - Smart Coffee Builder",
        EngagementTerminationApproved => "The engagement ended by mutual agreement - Smart Coffee Builder",
        EngagementTerminationRejected => "The early termination request was declined - Smart Coffee Builder",
        EngagementTerminationCancelled => "The early termination request was withdrawn - Smart Coffee Builder",
        ProjectReadyToClose => "The project is finished, waiting for you to close it - Smart Coffee Builder",
        ProjectCompleted => "The project is complete - Smart Coffee Builder",
        ProjectCancelled => "The project was cancelled - Smart Coffee Builder",
        ConstructionOverdue => "A construction item is behind schedule - Smart Coffee Builder",
        _ => "Notification - Smart Coffee Builder"
    };
}
