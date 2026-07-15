namespace SmartCoffeeBuilder.Service.DTOs.Responses.Survey;

public class SurveyResponse
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    public decimal Version { get; set; }
    public string ConditionNote { get; set; } = null!;
    public string? ReportUrl { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static SurveyResponse From(SmartCoffeeBuilder.Repository.Models.Survey e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        Version = e.Version,
        ConditionNote = e.ConditionNote,
        ReportUrl = e.ReportUrl,
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
