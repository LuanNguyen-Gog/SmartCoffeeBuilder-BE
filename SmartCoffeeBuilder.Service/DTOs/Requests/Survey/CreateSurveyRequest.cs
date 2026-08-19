namespace SmartCoffeeBuilder.Service.DTOs.Requests.Survey;

/// <summary>
/// Tạo bản khảo sát. Gửi ĐÚNG MỘT trong hai chỗ neo:
/// <list type="bullet">
/// <item><see cref="ApplyId"/> — khảo sát trong lúc ứng tuyển, KHÔNG cần engagement 'accepted'
/// (review 3: chủ quán xem khảo sát + báo giá của nhiều provider rồi mới chọn).</item>
/// <item><see cref="ProjectWorkingId"/> — khảo sát khi đã hợp tác (luồng cũ).</item>
/// </list>
/// </summary>
public class CreateSurveyRequest
{
    /// <summary>Engagement đã hợp tác. Bỏ trống nếu gửi <see cref="ApplyId"/>.</summary>
    public Guid? ProjectWorkingId { get; set; }

    /// <summary>Hồ sơ ứng tuyển. Bỏ trống nếu gửi <see cref="ProjectWorkingId"/>.</summary>
    public Guid? ApplyId { get; set; }

    /// <summary>
    /// Lịch hẹn đi khảo sát. Chỉ đặt lịch trước thì gửi mỗi trường này và để
    /// <see cref="ConditionNote"/> trống — ghi nhận hiện trạng điền sau khi đi.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>Thời điểm đã thực sự đi khảo sát (nếu khảo sát xong mới nhập).</summary>
    public DateTime? SurveyedAt { get; set; }

    /// <summary>Ghi chú hiện trạng mặt bằng. Để trống khi mới chỉ đặt lịch.</summary>
    public string? ConditionNote { get; set; }

    /// <summary>URL file báo cáo khảo sát (nếu có).</summary>
    public string? ReportUrl { get; set; }

    // KHÔNG có CreatedBy: người tạo lấy từ JWT. Client tự khai thì cột created_by mất giá trị
    // đối chứng — xem quy tắc Authorization trong CLAUDE.md.
}
