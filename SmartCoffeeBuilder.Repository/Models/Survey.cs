namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Bản khảo sát mặt bằng.
///
/// Neo vào ĐÚNG MỘT trong hai — enforce bằng CHECK <c>ck_surveys_target</c>:
/// <list type="bullet">
/// <item><see cref="ApplyId"/> — khảo sát provider làm khi ĐANG ỨNG TUYỂN, trước khi được chọn.
/// Đây là thay đổi của review 3: chủ quán cần xem khảo sát + báo giá của nhiều provider rồi mới
/// quyết định, nên khảo sát KHÔNG được đòi engagement 'accepted' nữa.</item>
/// <item><see cref="ProjectWorkingId"/> — khảo sát trong lúc đã hợp tác (luồng cũ, vẫn giữ).</item>
/// </list>
///
/// Phần "giá ước tính" đi kèm không nằm ở đây mà ở <see cref="Quotation"/> — báo giá cũng neo được
/// vào <c>Apply</c>, có sẵn hạng mục / đơn giá / điều kiện thanh toán. Nhân đôi cột giá ở survey chỉ
/// tạo ra hai con số có thể lệch nhau.
/// </summary>
public class Survey
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>project_providers.id</c>. null khi khảo sát gắn với hồ sơ ứng tuyển.</summary>
    public Guid? ProjectWorkingId { get; set; }

    /// <summary>FK -> <c>applies.id</c>. null khi khảo sát gắn với engagement đã hợp tác.</summary>
    public Guid? ApplyId { get; set; }

    // Bỏ cột Version: survey là bản ghi khảo sát độc lập, xếp theo CreatedAt là đủ —
    // không có nghiệp vụ nào đọc số hiệu phiên bản (khác Design/DesignVersion).

    /// <summary>
    /// Lịch hẹn đi khảo sát mặt bằng (review 3: "thêm thời gian khảo sát"). null = chưa hẹn/không
    /// cần hẹn. Đây là mốc DỰ KIẾN; <see cref="SurveyedAt"/> mới là lúc thực sự đi.
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>Thời điểm thực tế đã khảo sát — điền sau khi đi.</summary>
    public DateTime? SurveyedAt { get; set; }

    /// <summary>Hiện trạng ghi nhận được. Rỗng khi mới chỉ đặt lịch, chưa đi khảo sát.</summary>
    public string ConditionNote { get; set; } = string.Empty;

    public string? ReportUrl { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking? ProjectWorking { get; set; }
    public Apply? Apply { get; set; }
    public Account? CreatedByAccount { get; set; }
}
