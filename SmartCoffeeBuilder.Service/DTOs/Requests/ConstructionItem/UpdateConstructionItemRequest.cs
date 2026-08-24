namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;

/// <summary>Cập nhật thông tin milestone. Chuyển trạng thái dùng endpoint riêng (/status).</summary>
public class UpdateConstructionItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateOnly? StartAt { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualStartAt { get; set; }
    public DateOnly? ActualAt { get; set; }

    /// <summary>Chi phí nhân công dự tính.</summary>
    public decimal? EstimatedLaborCost { get; set; }

    /// <summary>Chi phí nhân công thực chi — điền sau khi hạng mục xong.</summary>
    public decimal? ActualLaborCost { get; set; }
}
