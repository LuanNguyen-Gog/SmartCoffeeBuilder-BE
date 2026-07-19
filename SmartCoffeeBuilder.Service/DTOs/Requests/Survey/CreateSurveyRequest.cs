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

    /// <summary>Account id của người tạo (provider).</summary>
    public long? CreatedBy { get; set; }
}
