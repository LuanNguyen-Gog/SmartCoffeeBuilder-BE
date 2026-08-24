using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;

/// <summary>Tạo milestone thi công. Chỉ tạo được khi engagement 'accepted' + có contract 'confirmed'.</summary>
public class CreateConstructionItemRequest
{
    [Required]
    public Guid ProjectWorkingId { get; set; }

    /// <summary>Milestone cha (phân cấp) — phải cùng engagement. Null nếu là milestone gốc.</summary>
    public Guid? ParentId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Nhóm hạng mục, ví dụ: Kết cấu, M&E, Nội thất.</summary>
    public string? Category { get; set; }

    /// <summary>Ngày dự kiến bắt đầu — cùng EstimateAt cho ra thời lượng của hạng mục (review 1.1).</summary>
    public DateOnly? StartAt { get; set; }

    public DateOnly? EstimateAt { get; set; }

    /// <summary>Chi phí nhân công / thuê thiết bị dự tính. Vật tư khai riêng qua /api/materials.</summary>
    public decimal? EstimatedLaborCost { get; set; }

    // KHÔNG có CreatedBy: người tạo lấy từ JWT. Client tự khai thì cột created_by mất giá trị
    // đối chứng — cùng lý do ConfirmContractOtpRequest đã bỏ field ConfirmedBy.
}
