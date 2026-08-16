using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một lần tạo link thanh toán payOS. Purpose quyết định giao dịch thuộc luồng nào:
/// - subscription: SubscriptionId bắt buộc, PostId/BoostDays null.
/// - post_boost: PostId + BoostDays bắt buộc, SubscriptionId null.
/// OrderCode là mã duy nhất gửi sang payOS — webhook trả về theo mã này.
/// </summary>
public class PaymentTransaction
{
    public Guid Id { get; set; }
    public Guid? SubscriptionId { get; set; }
    public Guid AccountId { get; set; }
    public PaymentPurpose Purpose { get; set; } = PaymentPurpose.subscription;
    public Guid? PostId { get; set; }
    public int? BoostDays { get; set; }
    public long OrderCode { get; set; }

    /// <summary>
    /// Nền tảng đã tạo link này. Lưu lại để KHÔNG tái sử dụng link của web cho mobile (và ngược lại) —
    /// returnUrl nhúng trong link payOS trỏ về sai domain thì FE bên kia không bắt được điểm quay về.
    /// </summary>
    public PaymentPlatform Platform { get; set; } = PaymentPlatform.web;
    public string PaymentLinkId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    public string QrCode { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.pending;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Subscription? Subscription { get; set; }
    public Post? Post { get; set; }
    public Account Account { get; set; } = null!;
}
