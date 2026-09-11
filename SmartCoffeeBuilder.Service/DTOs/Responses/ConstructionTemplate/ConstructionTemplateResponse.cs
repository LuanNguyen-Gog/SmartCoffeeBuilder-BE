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

/// <summary>
/// Một mẫu quy trình ĐÃ ĐƯỢC ÁP vào một engagement, nhìn từ phía dự án.
///
/// Đây là bản TÓM TẮT, không phải <see cref="ConstructionTemplateResponse"/>: chủ quán cần biết
/// nhà thầu đang chạy theo quy trình nào và quy trình đó phủ những hạng mục nào trong dự án CỦA
/// MÌNH — chứ không phải toàn bộ mẫu gốc, vốn là bí quyết nghề nhà thầu dùng lại cho khách khác.
/// Các mốc dưới đây vì thế đọc từ construction_items đã sinh, không đọc từ mẫu.
/// </summary>
public class AppliedConstructionTemplateResponse
{
    public Guid ConstructionTemplateId { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string ServiceKind { get; set; } = null!;

    /// <summary>true = mẫu chuẩn của hệ thống; false = mẫu riêng nhà thầu tự dựng.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Số hạng mục gốc (parent_id = null) trong dự án sinh ra từ mẫu này.</summary>
    public int AppliedItemCount { get; set; }

    /// <summary>Số hạng mục trong đó đã nghiệm thu xong — để vẽ thanh tiến độ của cả quy trình.</summary>
    public int CompletedItemCount { get; set; }

    /// <summary>Mốc nhà thầu bấm áp mẫu (created_at của lứa hạng mục đầu tiên).</summary>
    public DateTime AppliedAt { get; set; }

    public DateOnly? PlannedStartAt { get; set; }
    public DateOnly? PlannedFinishAt { get; set; }

    /// <summary>Tên các hạng mục sinh ra từ mẫu, theo đúng thứ tự kế hoạch.</summary>
    public List<string> ItemNames { get; set; } = new();
}
