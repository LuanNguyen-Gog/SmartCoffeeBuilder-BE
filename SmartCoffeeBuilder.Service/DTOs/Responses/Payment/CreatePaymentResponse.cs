namespace SmartCoffeeBuilder.Service.DTOs.Responses.Payment;

/// <summary>Thông tin link thanh toán payOS trả cho FE để redirect / hiển thị QR.</summary>
public class CreatePaymentResponse
{
    /// <summary>Mục đích giao dịch: subscription | post_boost.</summary>
    public string Purpose { get; set; } = null!;

    /// <summary>Có giá trị khi Purpose = subscription.</summary>
    public long? SubscriptionId { get; set; }

    /// <summary>Có giá trị khi Purpose = post_boost.</summary>
    public long? PostId { get; set; }

    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public string QrCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public long ExpiredAt { get; set; }
}
