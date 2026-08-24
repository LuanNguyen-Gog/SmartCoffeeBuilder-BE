using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ChangeOrder;

/// <summary>
/// Lập một khoản phát sinh chi phí ngoài báo giá đã chốt. Bên nào lập cũng được — owner báo đổi
/// phạm vi, provider báo đổi vật tư — nhưng BÊN KIA mới là bên duyệt.
/// </summary>
public class CreateChangeOrderRequest
{
    [Required]
    public Guid ProjectWorkingId { get; set; }

    /// <summary>extra_revision | scope_change | material_change | other.</summary>
    [Required]
    public string Kind { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = null!;

    /// <summary>Vì sao phát sinh — bên kia đọc cái này để quyết định duyệt hay không.</summary>
    [Required]
    public string Reason { get; set; } = null!;

    /// <summary>Số tiền phát sinh. Không âm; 0 hợp lệ khi chỉ ghi nhận thay đổi.</summary>
    public decimal Amount { get; set; }

    /// <summary>Bản thiết kế liên quan (thường đi với extra_revision).</summary>
    public Guid? DesignId { get; set; }

    /// <summary>Hạng mục thi công liên quan (thường đi với scope_change / material_change).</summary>
    public Guid? ConstructionItemId { get; set; }
}

/// <summary>Sửa khoản phát sinh — chỉ bên đã lập, và chỉ khi còn 'pending'.</summary>
public class UpdateChangeOrderRequest
{
    [MaxLength(255)]
    public string? Title { get; set; }
    public string? Reason { get; set; }
    public decimal? Amount { get; set; }
    public string? Kind { get; set; }
    public Guid? DesignId { get; set; }
    public Guid? ConstructionItemId { get; set; }
}

/// <summary>Bên kia từ chối khoản phát sinh.</summary>
public class RejectChangeOrderRequest
{
    [Required]
    public string RejectReason { get; set; } = null!;
}
