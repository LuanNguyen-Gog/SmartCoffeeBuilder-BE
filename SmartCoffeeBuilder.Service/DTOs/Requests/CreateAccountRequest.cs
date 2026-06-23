using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests;

public class CreateAccountRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    [Phone]
    public string? Phone { get; set; }

    /// <summary>owner | provider | admin</summary>
    [Required]
    public string Role { get; set; } = null!;

    /// <summary>active | inactive | banned | pending — mặc định active.</summary>
    public string? Status { get; set; }
}
