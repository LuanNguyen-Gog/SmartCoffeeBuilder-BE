using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Material;

/// <summary>Khai báo một vật tư vào bảng giá của engagement (công bố trước khi thi công).</summary>
public class CreateMaterialRequest
{
    [Required]
    public Guid ProjectWorkingId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>md | m2 | m3 | kg | litre | item | set | manday.</summary>
    [Required]
    public string Unit { get; set; } = null!;

    /// <summary>Đơn giá theo đơn vị tính. Không âm.</summary>
    public decimal UnitPrice { get; set; }

    public int? SortOrder { get; set; }
}

public class UpdateMaterialRequest
{
    [MaxLength(255)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public decimal? UnitPrice { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>
/// Gắn một vật tư trong bảng giá vào hạng mục hoặc task, kèm lượng DỰ TÍNH.
/// Gửi ĐÚNG MỘT trong <see cref="ConstructionItemId"/> / <see cref="ConstructionTaskId"/>.
/// </summary>
public class CreateConstructionMaterialRequest
{
    public Guid? ConstructionItemId { get; set; }
    public Guid? ConstructionTaskId { get; set; }

    [Required]
    public Guid MaterialId { get; set; }

    /// <summary>Lượng dự tính. Phải lớn hơn 0.</summary>
    public decimal EstimatedQuantity { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// Cập nhật một dòng vật tư. <see cref="ActualQuantity"/> là lượng THỰC TẾ — chỉ ghi được khi
/// công việc đã bắt đầu (xem MaterialService).
/// </summary>
public class UpdateConstructionMaterialRequest
{
    public decimal? EstimatedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }
    public string? Note { get; set; }
}
