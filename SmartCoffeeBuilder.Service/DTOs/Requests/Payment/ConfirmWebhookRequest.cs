using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Payment;

public class ConfirmWebhookRequest
{
    [Required]
    public string WebhookUrl { get; set; } = null!;
}
