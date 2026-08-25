namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTemplate;

/// <summary>
/// Provider dựng mẫu quy trình tái dùng: danh sách hạng mục, mỗi hạng mục kèm việc con và thời
/// lượng ước tính (ngày). Mẫu mới luôn là mẫu RIÊNG — chỉ admin mới đánh dấu công khai.
/// </summary>
public class CreateConstructionTemplateRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>design | construction | both. Bỏ trống = construction.</summary>
    public string? ServiceKind { get; set; }

    public List<ConstructionTemplateItemInput> Items { get; set; } = new();
}

public class ConstructionTemplateItemInput
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Nhóm hạng mục: MEP, trần nhà, nội thất…</summary>
    public string? Category { get; set; }

    /// <summary>Thời lượng dự kiến (ngày) của cả hạng mục.</summary>
    public int? EstimateDays { get; set; }

    public List<ConstructionTemplateTaskInput> Tasks { get; set; } = new();
}

public class ConstructionTemplateTaskInput
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int? EstimateDays { get; set; }
}

/// <summary>
/// Sắp lại thứ tự hạng mục trong mẫu. Nhận TOÀN BỘ id của mẫu theo thứ tự mong muốn — mẫu là
/// một danh sách phẳng nên không có nhóm anh em nào khác để phân biệt.
///
/// Thứ tự này quyết định thứ tự sinh hạng mục lúc áp mẫu, nên sửa mẫu ở đây KHÔNG đụng tới dự án
/// đã áp mẫu trước đó — áp mẫu vốn là copy một lần.
/// </summary>
public class ReorderConstructionTemplateItemsRequest
{
    public List<Guid> ItemIds { get; set; } = new();
}

/// <summary>Áp mẫu vào một engagement đã ký hợp đồng.</summary>
public class ApplyConstructionTemplateRequest
{
    public Guid ProjectWorkingId { get; set; }

    /// <summary>Ngày bắt đầu để giãn mốc estimate_at. Bỏ trống = hôm nay.</summary>
    public DateOnly? StartDate { get; set; }
}
