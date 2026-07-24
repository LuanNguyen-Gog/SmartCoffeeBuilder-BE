using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Payment;

public class CreateSubscriptionPaymentRequest
{
    [Required]
    public long PlanId { get; set; }

    /// <summary>"web" (mặc định) hoặc "mobile" — quyết định cặp returnUrl/cancelUrl gửi cho payOS.</summary>
    public string? Platform { get; set; }
}
