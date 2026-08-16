using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Account;

public class AccountResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static AccountResponse From(SmartCoffeeBuilder.Repository.Models.Account a) => new()
    {
        Id = a.Id,
        Email = a.Email,
        Phone = a.Phone,
        Role = a.Role.ToString(),
        Status = a.Status.ToString(),
        EmailVerifiedAt = a.EmailVerifiedAt,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
