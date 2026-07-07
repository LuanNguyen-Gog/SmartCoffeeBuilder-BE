namespace SmartCoffeeBuilder.Repository.Models;

public class Notification
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string Type { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
}
