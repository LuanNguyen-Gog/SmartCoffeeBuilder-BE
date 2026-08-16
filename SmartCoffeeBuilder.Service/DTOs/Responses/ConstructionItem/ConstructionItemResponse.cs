namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;

public class ConstructionItemResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public string Status { get; set; } = null!;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ConstructionItemResponse From(SmartCoffeeBuilder.Repository.Models.ConstructionItem e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        ParentId = e.ParentId,
        Name = e.Name,
        Description = e.Description,
        Category = e.Category,
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
