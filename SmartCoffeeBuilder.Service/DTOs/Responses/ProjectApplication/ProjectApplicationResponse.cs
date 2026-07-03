namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectApplication;

public class ProjectApplicationResponse
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public string? PostTitle { get; set; }
    public long? ProjectId { get; set; }
    public long ProviderId { get; set; }
    public string? ProviderDisplayName { get; set; }
    public string Proposal { get; set; } = null!;
    public int? EstimatedDurationDays { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ProjectApplicationResponse From(SmartCoffeeBuilder.Repository.Models.ProjectApplication a) => new()
    {
        Id = a.Id,
        PostId = a.PostId,
        PostTitle = a.Post?.Title,
        ProjectId = a.Post?.ProjectId,
        ProviderId = a.ProviderId,
        ProviderDisplayName = a.Provider?.DisplayName,
        Proposal = a.Proposal,
        EstimatedDurationDays = a.EstimatedDurationDays,
        Status = a.Status.ToString(),
        SubmittedAt = a.SubmittedAt,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
