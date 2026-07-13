using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;

/// <summary>Tạo milestone thi công. Chỉ tạo được khi engagement 'accepted' + có contract 'confirmed'.</summary>
public class CreateConstructionItemRequest
{
    [Required]
    public long ProjectProviderId { get; set; }

    /// <summary>Milestone cha (phân cấp) — phải cùng engagement. Null nếu là milestone gốc.</summary>
    public long? ParentId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Nhóm hạng mục, ví dụ: Kết cấu, M&E, Nội thất.</summary>
    public string? Category { get; set; }

    public DateOnly? EstimateAt { get; set; }

    /// <summary>Account id của người tạo (constructor).</summary>
    public long? CreatedBy { get; set; }
}
