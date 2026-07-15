namespace SmartCoffeeBuilder.Service.DTOs.Responses.Post;

public class PostResponse
{
    public long Id { get; set; }
    public long ProjectShopOwnerId { get; set; }
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

    public static PostResponse From(SmartCoffeeBuilder.Repository.Models.Post p) => new()
    {
        Id = p.Id,
        ProjectShopOwnerId = p.ProjectShopOwnerId,
        ProjectName = p.ProjectShopOwner?.Name,
        ProjectAddress = p.ProjectShopOwner?.Address,
        ProjectBudget = p.ProjectShopOwner?.Budget,
        ProjectAreaM2 = p.ProjectShopOwner?.AreaM2,
        ServiceKind = p.ServiceKind.ToString(),
        Title = p.Title,
        Description = p.Description,
        Status = p.Status.ToString(),
        SubmissionDeadline = p.SubmissionDeadline,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
