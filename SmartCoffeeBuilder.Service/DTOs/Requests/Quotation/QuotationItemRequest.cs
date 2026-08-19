namespace SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;

/// <summary>
/// Một dòng hạng mục gửi kèm khi tạo/sửa báo giá. Thành tiền KHÔNG nhận từ client —
/// service tự tính Quantity × UnitPrice để tổng báo giá luôn khớp với từng dòng.
/// </summary>
public class QuotationItemRequest
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Unit { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Note { get; set; }
}
