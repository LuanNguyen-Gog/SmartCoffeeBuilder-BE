namespace SmartCoffeeBuilder.Service.DTOs.Responses.Issue;

public class IssueResponse
{
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public long? ConstructionItemId { get; set; }
    public long IssueTypeId { get; set; }
    public string? IssueTypeName { get; set; }
    public string? Cause { get; set; }
    public string? Reason { get; set; }
    public string? Solution { get; set; }
    public string? IssueImage { get; set; }
    public string? ConfirmImage { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public string Status { get; set; } = null!;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static IssueResponse From(SmartCoffeeBuilder.Repository.Models.Issue e) => new()
    {
        Id = e.Id,
        ProjectProviderId = e.ProjectProviderId,
        ConstructionItemId = e.ConstructionItemId,
        IssueTypeId = e.IssueTypeId,
        IssueTypeName = e.IssueType?.Name,
        Cause = e.Cause,
        Reason = e.Reason,
        Solution = e.Solution,
        IssueImage = e.IssueImage,
        ConfirmImage = e.ConfirmImage,
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
