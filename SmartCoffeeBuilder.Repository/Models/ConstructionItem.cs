using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ConstructionItem
{
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public long? ParentId { get; set; } // phân cấp
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    // v5: bỏ IsDone — trạng thái hoàn thành chỉ đọc từ Status = completed (một nguồn sự thật).
    public ItemStatus Status { get; set; } = ItemStatus.pending;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectProvider ProjectProvider { get; set; } = null!;
    public ConstructionItem? Parent { get; set; }
    public ICollection<ConstructionItem> Children { get; set; } = new List<ConstructionItem>();
    public Account? CreatedByAccount { get; set; }
    public ICollection<ConstructionTask> Tasks { get; set; } = new List<ConstructionTask>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
