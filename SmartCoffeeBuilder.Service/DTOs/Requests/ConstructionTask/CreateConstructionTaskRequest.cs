using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;

/// <summary>Tạo task nhỏ trong một milestone (construction_item).</summary>
public class CreateConstructionTaskRequest
{
    [Required]
    public Guid ConstructionItemId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Ảnh hiện trường.</summary>
    public string? ImageUrl { get; set; }

    public DateOnly? EstimateAt { get; set; }

    // KHÔNG có CreatedBy: người tạo lấy từ JWT (xem CreateConstructionItemRequest).
}
