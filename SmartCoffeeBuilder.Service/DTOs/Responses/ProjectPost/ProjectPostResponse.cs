using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses;

public class ProjectPostResponse
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectAddress { get; set; }
    public decimal? ProjectBudget { get; set; }
    public decimal? ProjectAreaM2 { get; set; }
    public string ServiceKind { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? SubmissionDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ProjectPostResponse From(ProjectPost p) => new()
    {
        Id = p.Id,
        ProjectId = p.ProjectId,
        ProjectName = p.Project?.Name,
        ProjectAddress = p.Project?.Address,
        ProjectBudget = p.Project?.Budget,
        ProjectAreaM2 = p.Project?.AreaM2,
        ServiceKind = p.ServiceKind.ToString(),
        Title = p.Title,
        Description = p.Description,
        Status = p.Status.ToString(),
        SubmissionDeadline = p.SubmissionDeadline,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
