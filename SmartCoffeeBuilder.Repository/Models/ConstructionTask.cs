using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Việc nhỏ bên trong một milestone (construction_item). Dùng chung item_status với milestone.</summary>
public class ConstructionTask
{
    public Guid Id { get; set; }
    public Guid ConstructionItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; } // ảnh hiện trường
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public string? Reason { get; set; }
    public ItemStatus Status { get; set; } = ItemStatus.pending;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ConstructionItem ConstructionItem { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }

    /// <summary>Vật tư dự tính / thực dùng của task này (review 3).</summary>
    public ICollection<ConstructionMaterial> Materials { get; set; } = new List<ConstructionMaterial>();
}
