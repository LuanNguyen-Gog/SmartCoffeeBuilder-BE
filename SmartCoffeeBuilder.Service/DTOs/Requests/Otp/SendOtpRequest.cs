using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Otp;

public class SendOtpRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
}
