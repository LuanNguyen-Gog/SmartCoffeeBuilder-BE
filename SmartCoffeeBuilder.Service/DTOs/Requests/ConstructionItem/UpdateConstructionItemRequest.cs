namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;

/// <summary>Cập nhật thông tin milestone. Chuyển trạng thái dùng endpoint riêng (/status).</summary>
public class UpdateConstructionItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateOnly? EstimateAt { get; set; }
}
