namespace SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;

/// <summary>
/// Một đợt trong điều kiện thanh toán. Gửi <see cref="Percentage"/> (service tự quy ra tiền theo
/// tổng báo giá) HOẶC <see cref="Amount"/> khi đợt ghi bằng số tuyệt đối. Gửi cả hai thì % thắng.
/// </summary>
public class QuotationPaymentTermRequest
{
    public string Name { get; set; } = null!;
    public decimal? Percentage { get; set; }
    public decimal? Amount { get; set; }
    public string? Condition { get; set; }
}
