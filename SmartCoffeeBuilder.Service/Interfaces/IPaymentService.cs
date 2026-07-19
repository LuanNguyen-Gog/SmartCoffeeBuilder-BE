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
    Task<CreatePaymentResponse> CreateSubscriptionPaymentAsync(long accountId, CreateSubscriptionPaymentRequest request);

    /// <summary>Tạo link payOS trả phí đẩy bài đăng lên đầu danh sách — chỉ chủ quán sở hữu bài.</summary>
    Task<CreatePaymentResponse> CreatePostBoostPaymentAsync(long accountId, CreatePostBoostRequest request);
    Task<SubscriptionResponse?> GetActiveSubscriptionAsync(long accountId);
    Task<ICollection<SubscriptionResponse>> GetSubscriptionHistoryAsync(long accountId);
    Task<PaymentStatusResponse> GetPaymentStatusAsync(long? orderCode, string? paymentLinkId);

    /// <summary>FE gọi khi user bấm huỷ / bị redirect về cancelUrl — huỷ giao dịch pending.</summary>
    Task<PaymentStatusResponse> CancelPaymentAsync(long orderCode);

    /// <summary>payOS gọi endpoint webhook — verify chữ ký rồi chốt trạng thái giao dịch.</summary>
    Task<string> HandleWebhookAsync(WebhookType webhook);

    /// <summary>Đăng ký webhook URL với payOS (admin gọi một lần khi deploy).</summary>
    Task ConfirmWebhookAsync(string webhookUrl);

    /// <summary>Job nền: chuyển subscription active quá EndDate sang expired, dọn giao dịch pending quá hạn link.</summary>
    Task ExpireOverdueSubscriptionsAsync();
}
