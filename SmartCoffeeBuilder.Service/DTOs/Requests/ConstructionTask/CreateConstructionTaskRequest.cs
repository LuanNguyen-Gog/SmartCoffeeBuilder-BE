using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;

/// <summary>Tạo task nhỏ trong một milestone (construction_item).</summary>
public class CreateConstructionTaskRequest
{
    [Required]
    public long ConstructionItemId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Ảnh hiện trường.</summary>
    public string? ImageUrl { get; set; }

    public DateOnly? EstimateAt { get; set; }

    /// <summary>Account id của người tạo (constructor).</summary>
    public long? CreatedBy { get; set; }
}
