namespace SmartCoffeeBuilder.Repository.Models;

public class RefreshToken
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Account Account { get; set; } = null!;

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}
