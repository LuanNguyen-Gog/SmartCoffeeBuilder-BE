namespace SmartCoffeeBuilder.Service.DTOs.Requests.Survey;

public class UpdateSurveyRequest
{
    /// <summary>Dời lịch hẹn khảo sát.</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>Đánh dấu đã đi khảo sát lúc nào.</summary>
    public DateTime? SurveyedAt { get; set; }

    public string? ConditionNote { get; set; }
    public string? ReportUrl { get; set; }
}
