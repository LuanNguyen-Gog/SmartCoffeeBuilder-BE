namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTask;

public class ConstructionTaskResponse
{
    public long Id { get; set; }
    public long ConstructionItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = null!;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ConstructionTaskResponse From(SmartCoffeeBuilder.Repository.Models.ConstructionTask e) => new()
    {
        Id = e.Id,
        ConstructionItemId = e.ConstructionItemId,
        Name = e.Name,
        Description = e.Description,
        ImageUrl = e.ImageUrl,
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        Reason = e.Reason,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
