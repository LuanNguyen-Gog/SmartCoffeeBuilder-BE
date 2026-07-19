namespace SmartCoffeeBuilder.Service.DTOs.Responses.Payment;

/// <summary>Thông tin link thanh toán payOS trả cho FE để redirect / hiển thị QR.</summary>
public class CreatePaymentResponse
{
    public long SubscriptionId { get; set; }
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public string QrCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public long ExpiredAt { get; set; }
}
