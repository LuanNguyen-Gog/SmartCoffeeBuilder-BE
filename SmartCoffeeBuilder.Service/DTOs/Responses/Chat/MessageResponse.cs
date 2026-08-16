using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Chat;

/// <summary>Một tin nhắn trong thread. Sender build bằng <see cref="SenderInfoFactory"/> ở service layer.</summary>
public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public SenderInfo Sender { get; set; } = null!;

    /// <summary>Nội dung văn bản — null nếu tin chỉ chứa file.</summary>
    public string? Body { get; set; }

    public List<MessageAttachmentResponse> Attachments { get; set; } = [];
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Map các field cơ bản; Sender phải được build và gán riêng bằng
    /// <c>SenderInfoFactory.BuildAsync(...)</c> sau khi gọi hàm này.
    /// </summary>
    public static MessageResponse From(Message m) => new()
    {
        Id = m.Id,
        ConversationId = m.ConversationId,
        SenderId = m.SenderId,
        // Sender sẽ được service gán sau — không thể build tại đây vì cần async lookup ShopOwner/Provider.
        Sender = new SenderInfo { AccountId = m.SenderId, Role = m.Sender.Role.ToString() },
        Body = m.Body,
        Attachments = m.Attachments.Select(MessageAttachmentResponse.From).ToList(),
        SentAt = m.SentAt
    };
}
