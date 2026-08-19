namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Một hạng mục (milestone) trong mẫu — áp vào dự án sẽ thành <see cref="ConstructionItem"/>.</summary>
public class ConstructionTemplateItem
{
    public Guid Id { get; set; }
    public Guid ConstructionTemplateId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Nhóm hạng mục: MEP, trần nhà, nội thất… (review 3 gọi là "hạng mục liên quan").</summary>
    public string? Category { get; set; }

    /// <summary>Thời lượng dự kiến (ngày) — dùng để giãn mốc estimate_at khi áp mẫu.</summary>
    public int? EstimateDays { get; set; }

    public int SortOrder { get; set; }

    public ConstructionTemplate ConstructionTemplate { get; set; } = null!;
    public ICollection<ConstructionTemplateTask> Tasks { get; set; } = new List<ConstructionTemplateTask>();
}
