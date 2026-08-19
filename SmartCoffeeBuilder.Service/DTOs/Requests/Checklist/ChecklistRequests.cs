namespace SmartCoffeeBuilder.Service.DTOs.Requests.Checklist;

/// <summary>
/// Provider lập checklist nghiệm thu cho ĐÚNG MỘT hạng mục: bản thiết kế (designId) hoặc hạng mục
/// thi công (constructionItemId). Nhập được nhiều mục một lần vì checklist thường viết theo cụm.
/// </summary>
public class CreateChecklistItemsRequest
{
    public Guid? DesignId { get; set; }
    public Guid? ConstructionItemId { get; set; }
    public List<ChecklistItemInput> Items { get; set; } = new();
}

public class ChecklistItemInput
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Mặc định true — mục bắt buộc phải đạt mới coi là nghiệm thu xong.</summary>
    public bool? IsRequired { get; set; }
}

public class UpdateChecklistItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsRequired { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>Owner chấm một mục: passed | failed. Chấm 'failed' bắt buộc kèm ghi chú cần sửa gì.</summary>
public class CheckChecklistItemRequest
{
    public string Status { get; set; } = null!;
    public string? Note { get; set; }

    /// <summary>ObjectName ảnh minh chứng (upload trước qua api/files) — tuỳ chọn.</summary>
    public string? EvidenceUrl { get; set; }
}

/// <summary>Provider đính minh chứng cho một mục mà không chấm điểm.</summary>
public class AttachChecklistEvidenceRequest
{
    public string EvidenceUrl { get; set; } = null!;
}
