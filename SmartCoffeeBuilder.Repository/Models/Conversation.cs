namespace SmartCoffeeBuilder.Repository.Models;

public class Conversation
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    public string? Topic { get; set; }
    public DateTime CreatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
