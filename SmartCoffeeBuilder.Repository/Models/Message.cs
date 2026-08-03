namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một tin nhắn trong <see cref="Conversation"/>. Body có thể rỗng/null nếu là
/// tin chỉ chứa file đính kèm (ảnh / tài liệu). Cascade-delete theo conversation.
/// </summary>
public class Message
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    /// <summary>Nội dung văn bản — có thể null/rỗng nếu chỉ gửi file.</summary>
    public string? Body { get; set; }
    public DateTime SentAt { get; set; }

    public Conversation Conversation { get; set; } = null!;
    public Account Sender { get; set; } = null!;
    public ICollection<MessageAttachment> Attachments { get; set; } = new List<MessageAttachment>();
}
