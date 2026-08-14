using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Survey;

public class CreateSurveyRequest
{
    [Required]
    public long ProjectWorkingId { get; set; }

    /// <summary>Ghi chú hiện trạng mặt bằng.</summary>
    [Required]
    public string ConditionNote { get; set; } = null!;

    /// <summary>URL file báo cáo khảo sát (nếu có).</summary>
    public string? ReportUrl { get; set; }

    // KHÔNG có CreatedBy: người tạo lấy từ JWT. Client tự khai thì cột created_by mất giá trị
    // đối chứng — xem quy tắc Authorization trong CLAUDE.md.
}
