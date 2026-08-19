using SmartCoffeeBuilder.Service.Utils;
using Entity = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Checklist;

/// <summary>Một mục nghiệm thu kèm kết quả chấm của chủ quán.</summary>
public class ChecklistItemResponse
{
    public Guid Id { get; set; }
    public Guid? DesignId { get; set; }
    public Guid? ConstructionItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }

    /// <summary>pending | passed | failed</summary>
    public string Status { get; set; } = null!;

    public string? EvidenceUrl { get; set; }
    public string? EvidenceViewUrl { get; set; }
    public string? Note { get; set; }
    public Guid? CheckedBy { get; set; }
    public DateTime? CheckedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ChecklistItemResponse From(Entity.ChecklistItem e) => new()
    {
        Id = e.Id,
        DesignId = e.DesignId,
        ConstructionItemId = e.ConstructionItemId,
        Name = e.Name,
        Description = e.Description,
        SortOrder = e.SortOrder,
        IsRequired = e.IsRequired,
        Status = e.Status.ToString(),
        EvidenceUrl = e.EvidenceUrl,
        EvidenceViewUrl = MediaUrl.Resolve(e.EvidenceUrl),
        Note = e.Note,
        CheckedBy = e.CheckedBy,
        CheckedAt = e.CheckedAt,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
