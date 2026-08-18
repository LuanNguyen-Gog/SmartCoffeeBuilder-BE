using Net.payOS.Types;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.Payment;
using SmartCoffeeBuilder.Service.DTOs.Responses.Payment;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Thanh toán phí nền tảng (subscription) qua payOS.
/// Luồng: tạo link → FE redirect sang payOS → payOS gọi webhook → kích hoạt subscription.
/// </summary>
public interface IPaymentService
{
    Task<ICollection<SubscriptionPlanResponse>> GetPlansAsync(AccountRole? targetRole = null);
    Task<CreatePaymentResponse> CreateSubscriptionPaymentAsync(Guid accountId, CreateSubscriptionPaymentRequest request);

    /// <summary>Tạo link payOS trả phí đẩy bài đăng lên đầu danh sách — chỉ chủ quán sở hữu bài.</summary>
    Task<CreatePaymentResponse> CreatePostBoostPaymentAsync(Guid accountId, CreatePostBoostRequest request);
    Task<SubscriptionResponse?> GetActiveSubscriptionAsync(Guid accountId);
    Task<ICollection<SubscriptionResponse>> GetSubscriptionHistoryAsync(Guid accountId);
    /// <summary>Chỉ chủ giao dịch mới xem được — orderCode dễ đoán nên bắt buộc kiểm tra quyền sở hữu.</summary>
    Task<PaymentStatusResponse> GetPaymentStatusAsync(Guid accountId, long? orderCode, string? paymentLinkId);

    /// <summary>
    /// FE gọi khi user bấm huỷ / bị redirect về cancelUrl — huỷ giao dịch pending VÀ huỷ luôn link
    /// phía payOS (tránh trường hợp huỷ nội bộ xong link cũ vẫn thanh toán được). Chỉ chủ giao dịch
    /// mới huỷ được.
    /// </summary>
    Task<PaymentStatusResponse> CancelPaymentAsync(Guid accountId, long orderCode);

    /// <summary>payOS gọi endpoint webhook — verify chữ ký rồi chốt trạng thái giao dịch.</summary>
    Task<string> HandleWebhookAsync(WebhookType webhook);

    /// <summary>Đăng ký webhook URL với payOS (admin gọi một lần khi deploy).</summary>
    Task ConfirmWebhookAsync(string webhookUrl);

    /// <summary>Job nền: chuyển subscription active quá EndDate sang expired, dọn giao dịch pending quá hạn link.</summary>
    Task ExpireOverdueSubscriptionsAsync();
}
