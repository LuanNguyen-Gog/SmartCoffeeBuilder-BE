using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    [Phone]
    public string? Phone { get; set; }

    /// <summary>owner | designer | constructor</summary>
    [Required]
    public string Role { get; set; } = null!;
}
