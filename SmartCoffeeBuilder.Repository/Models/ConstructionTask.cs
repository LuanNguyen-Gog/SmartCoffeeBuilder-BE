using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Việc nhỏ bên trong một milestone (construction_item). Dùng chung item_status với milestone.</summary>
public class ConstructionTask
{
    public long Id { get; set; }
    public long ConstructionItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; } // ảnh hiện trường
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public string? Reason { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.pending;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ConstructionItem ConstructionItem { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
}
