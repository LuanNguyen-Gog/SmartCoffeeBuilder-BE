using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTask;

public class ConstructionTaskResponse
{
    public long Id { get; set; }
    public long ConstructionItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>ObjectName ảnh hiện trường trên bucket — giá trị lưu trong DB.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>URL public tuyệt đối của ảnh hiện trường — FE dùng thẳng làm img src.</summary>
    public string? ImageViewUrl { get; set; }
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
        ImageViewUrl = MediaUrl.Resolve(e.ImageUrl),
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        Reason = e.Reason,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
