using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Gói phí nền tảng (subscription) — nguồn thu chính của platform.
/// TargetRole quyết định gói dành cho shop owner hay service provider.
/// </summary>
public class SubscriptionPlan
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public AccountRole TargetRole { get; set; }
    public decimal Price { get; set; }
    public int DurationInDays { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
