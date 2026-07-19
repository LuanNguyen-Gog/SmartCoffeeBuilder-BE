using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Payment;

public class CreateSubscriptionPaymentRequest
{
    [Required]
    public long PlanId { get; set; }
}
