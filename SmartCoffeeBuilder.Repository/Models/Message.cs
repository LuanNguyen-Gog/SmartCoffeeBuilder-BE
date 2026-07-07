namespace SmartCoffeeBuilder.Repository.Models;

public class Message
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string Body { get; set; } = null!;
    public DateTime SentAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Account Sender { get; set; } = null!;
}
