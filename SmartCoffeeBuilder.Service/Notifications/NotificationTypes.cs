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

    /// <summary>Tên file template email (không đuôi .html) tương ứng mỗi loại.</summary>
    public static string TemplateFor(string type) => type switch
    {
        ApplicationReceived => "ApplicationReceivedEmail",
        ApplicationAccepted => "ApplicationAcceptedEmail",
        ApplicationRejected => "ApplicationRejectedEmail",
        _ => "NotificationEmail" // template chung dự phòng
    };

    /// <summary>Subject email mặc định cho mỗi loại.</summary>
    public static string SubjectFor(string type) => type switch
    {
        ApplicationReceived => "Hồ sơ ứng tuyển mới - Smart Coffee Builder",
        ApplicationAccepted => "Hồ sơ của bạn đã được chấp nhận - Smart Coffee Builder",
        ApplicationRejected => "Kết quả hồ sơ ứng tuyển - Smart Coffee Builder",
        _ => "Thông báo - Smart Coffee Builder"
    };
}
