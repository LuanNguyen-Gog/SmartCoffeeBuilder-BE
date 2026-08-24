using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Survey;

public class SurveyResponse
{
    public Guid Id { get; set; }

    /// <summary>Engagement chứa khảo sát này — null khi khảo sát gắn với hồ sơ ứng tuyển.</summary>
    public Guid? ProjectWorkingId { get; set; }

    /// <summary>Hồ sơ ứng tuyển chứa khảo sát này — null khi khảo sát gắn với engagement.</summary>
    public Guid? ApplyId { get; set; }

    /// <summary>Lịch hẹn đi khảo sát (dự kiến).</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>Thời điểm đã thực sự khảo sát.</summary>
    public DateTime? SurveyedAt { get; set; }

    public string ConditionNote { get; set; } = string.Empty;

    /// <summary>ObjectName file báo cáo khảo sát trên bucket — giá trị lưu trong DB.</summary>
    public string? ReportUrl { get; set; }

    /// <summary>URL public tuyệt đối của file báo cáo — FE dùng thẳng để xem/tải.</summary>
    public string? ReportViewUrl { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static SurveyResponse From(SmartCoffeeBuilder.Repository.Models.Survey e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        ApplyId = e.ApplyId,
        ScheduledAt = e.ScheduledAt,
        SurveyedAt = e.SurveyedAt,
        ConditionNote = e.ConditionNote,
        ReportUrl = e.ReportUrl,
        ReportViewUrl = MediaUrl.Resolve(e.ReportUrl),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
