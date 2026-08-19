namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Một việc con trong hạng mục mẫu — áp vào dự án sẽ thành <see cref="ConstructionTask"/>.</summary>
public class ConstructionTemplateTask
{
    public Guid Id { get; set; }
    public Guid ConstructionTemplateItemId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Thời lượng dự kiến (ngày) của riêng việc này.</summary>
    public int? EstimateDays { get; set; }

    public int SortOrder { get; set; }

    public ConstructionTemplateItem ConstructionTemplateItem { get; set; } = null!;
}
