using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Account
{
    public long Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = null!;
    public AccountRole Role { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.active;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ShopOwner? ShopOwner { get; set; }
    public ServiceProvider? ServiceProvider { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Otp> Otps { get; set; } = new List<Otp>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
