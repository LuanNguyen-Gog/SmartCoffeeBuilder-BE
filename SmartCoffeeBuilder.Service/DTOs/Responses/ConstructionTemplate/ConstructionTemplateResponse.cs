using Entity = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTemplate;

public class ConstructionTemplateResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ServiceKind { get; set; } = null!;
    public bool IsPublic { get; set; }
    public Guid? CreatedBy { get; set; }

    /// <summary>Tổng thời lượng dự kiến của mẫu (cộng dồn EstimateDays các hạng mục).</summary>
    public int TotalEstimateDays { get; set; }

    public List<ConstructionTemplateItemResponse> Items { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    public static ConstructionTemplateResponse From(Entity.ConstructionTemplate e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        ServiceKind = e.ServiceKind.ToString(),
        IsPublic = e.IsPublic,
        CreatedBy = e.CreatedBy,
        TotalEstimateDays = e.Items.Sum(i => i.EstimateDays ?? 0),
        Items = e.Items.OrderBy(i => i.SortOrder).Select(ConstructionTemplateItemResponse.From).ToList(),
        CreatedAt = e.CreatedAt
    };
}

public class ConstructionTemplateItemResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? EstimateDays { get; set; }
    public int SortOrder { get; set; }
    public List<ConstructionTemplateTaskResponse> Tasks { get; set; } = new();

    public static ConstructionTemplateItemResponse From(Entity.ConstructionTemplateItem e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        Category = e.Category,
        EstimateDays = e.EstimateDays,
        SortOrder = e.SortOrder,
        Tasks = e.Tasks.OrderBy(t => t.SortOrder).Select(ConstructionTemplateTaskResponse.From).ToList()
    };
}

public class ConstructionTemplateTaskResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? EstimateDays { get; set; }
    public int SortOrder { get; set; }

    public static ConstructionTemplateTaskResponse From(Entity.ConstructionTemplateTask e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        EstimateDays = e.EstimateDays,
        SortOrder = e.SortOrder
    };
}

/// <summary>Kết quả áp mẫu vào dự án — báo đã sinh bao nhiêu hạng mục/việc và mốc kết thúc dự kiến.</summary>
public class ApplyTemplateResponse
{
    public Guid ConstructionTemplateId { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public int CreatedItems { get; set; }
    public int CreatedTasks { get; set; }
    public DateOnly PlannedFinishAt { get; set; }
}
