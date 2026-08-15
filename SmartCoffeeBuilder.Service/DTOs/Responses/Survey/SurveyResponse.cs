using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Survey;

public class SurveyResponse
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    public string ConditionNote { get; set; } = null!;
    /// <summary>ObjectName file báo cáo khảo sát trên bucket — giá trị lưu trong DB.</summary>
    public string? ReportUrl { get; set; }
    /// <summary>URL public tuyệt đối của file báo cáo — FE dùng thẳng để xem/tải.</summary>
    public string? ReportViewUrl { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static SurveyResponse From(SmartCoffeeBuilder.Repository.Models.Survey e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        ConditionNote = e.ConditionNote,
        ReportUrl = e.ReportUrl,
        ReportViewUrl = MediaUrl.Resolve(e.ReportUrl),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
