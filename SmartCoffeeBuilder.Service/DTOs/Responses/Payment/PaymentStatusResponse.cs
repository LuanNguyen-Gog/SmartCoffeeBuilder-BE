using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Payment;

/// <summary>Trạng thái giao dịch để FE polling sau khi redirect về từ payOS.</summary>
public class PaymentStatusResponse
{
    public bool Success { get; set; }

    /// <summary>true khi giao dịch đã ở trạng thái cuối (paid/cancelled/failed) — FE dừng polling.</summary>
    public bool IsFinal { get; set; }

    public PaymentTransactionStatus Status { get; set; }
    public PaymentPurpose Purpose { get; set; }
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = null!;

    /// <summary>Có giá trị khi Purpose = subscription.</summary>
    public Guid? SubscriptionId { get; set; }
    public SubscriptionStatus? SubscriptionStatus { get; set; }

    /// <summary>Có giá trị khi Purpose = post_boost.</summary>
    public Guid? PostId { get; set; }
    public DateTime? PostBoostedUntil { get; set; }

    public decimal Amount { get; set; }
    public string Message { get; set; } = null!;

    public static PaymentStatusResponse From(SmartCoffeeBuilder.Repository.Models.PaymentTransaction t) => new()
    {
        Success = t.Status == PaymentTransactionStatus.paid,
        IsFinal = t.Status != PaymentTransactionStatus.pending,
        Status = t.Status,
        Purpose = t.Purpose,
        OrderCode = t.OrderCode,
        PaymentLinkId = t.PaymentLinkId,
        SubscriptionId = t.SubscriptionId,
        SubscriptionStatus = t.Subscription?.Status,
        PostId = t.PostId,
        PostBoostedUntil = t.Post?.BoostedUntil,
        Amount = t.Amount,
        Message = t.Status switch
        {
            PaymentTransactionStatus.paid => t.Purpose == PaymentPurpose.post_boost
                ? "Thanh toán thành công — bài đăng đã được đẩy lên nổi bật."
                : "Thanh toán thành công — gói đã được kích hoạt.",
            PaymentTransactionStatus.cancelled => "Giao dịch đã bị huỷ.",
            PaymentTransactionStatus.failed => "Thanh toán thất bại.",
            _ => "Đang chờ payOS xác nhận thanh toán."
        }
    };
}
