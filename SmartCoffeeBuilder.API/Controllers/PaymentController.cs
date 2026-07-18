using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.Payment;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Thanh toán phí nền tảng (subscription) qua payOS.
/// Luồng FE: GET plans → POST subscriptions (nhận checkoutUrl) → redirect sang payOS
/// → payOS gọi POST webhook để chốt trạng thái → FE quay về returnUrl/cancelUrl
/// và polling GET status (hoặc gọi POST cancel khi user huỷ).
/// </summary>
[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    private long GetAccountId()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        return long.Parse(accountId);
    }

    /// <summary>Danh sách gói phí nền tảng đang mở bán (lọc theo role nếu truyền targetRole).</summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlans([FromQuery] AccountRole? targetRole)
    {
        var result = await _paymentService.GetPlansAsync(targetRole);
        return Ok(result);
    }

    /// <summary>Tạo link thanh toán payOS cho gói subscription — account lấy từ JWT.</summary>
    [HttpPost("subscriptions")]
    [Authorize]
    public async Task<IActionResult> CreateSubscriptionPayment([FromBody] CreateSubscriptionPaymentRequest request)
    {
        var result = await _paymentService.CreateSubscriptionPaymentAsync(GetAccountId(), request);
        return Ok(result);
    }

    /// <summary>Gói đang active (còn hạn) của account hiện tại — null nếu chưa mua.</summary>
    [HttpGet("subscriptions/me/active")]
    [Authorize]
    public async Task<IActionResult> GetMyActiveSubscription()
    {
        var result = await _paymentService.GetActiveSubscriptionAsync(GetAccountId());
        return Ok(result);
    }

    /// <summary>Lịch sử mua gói của account hiện tại.</summary>
    [HttpGet("subscriptions/me")]
    [Authorize]
    public async Task<IActionResult> GetMySubscriptionHistory()
    {
        var result = await _paymentService.GetSubscriptionHistoryAsync(GetAccountId());
        return Ok(result);
    }

    /// <summary>FE polling trạng thái giao dịch sau khi redirect về từ payOS.</summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPaymentStatus([FromQuery] long? orderCode, [FromQuery] string? paymentLinkId)
    {
        var result = await _paymentService.GetPaymentStatusAsync(orderCode, paymentLinkId);
        return Ok(result);
    }

    /// <summary>FE gọi khi user huỷ thanh toán (redirect về cancelUrl). Idempotent.</summary>
    [HttpPost("cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> CancelPayment([FromQuery] long orderCode)
    {
        var result = await _paymentService.CancelPaymentAsync(orderCode);
        return Ok(result);
    }

    /// <summary>Endpoint payOS gọi để báo kết quả thanh toán (đã verify chữ ký trong service).</summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook([FromBody] WebhookType webhook)
    {
        var message = await _paymentService.HandleWebhookAsync(webhook);
        return Ok(new { message });
    }

    /// <summary>Đăng ký webhook URL với payOS — admin gọi một lần sau khi deploy.</summary>
    [HttpPost("webhook/confirm")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ConfirmWebhook([FromBody] ConfirmWebhookRequest request)
    {
        await _paymentService.ConfirmWebhookAsync(request.WebhookUrl);
        return Ok(new { message = "Xác nhận webhook URL với payOS thành công." });
    }
}
