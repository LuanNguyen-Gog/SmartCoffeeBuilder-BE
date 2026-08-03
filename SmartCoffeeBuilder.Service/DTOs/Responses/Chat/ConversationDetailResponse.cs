using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Chat;

/// <summary>
/// Chi tiết một thread kèm danh sách message đã được sắp xếp (SentAt ASC).
/// Service phân trang theo <c>pageNumber</c>/<c>pageSize</c> rồi truyền vào <see cref="From"/>.
/// </summary>
public class ConversationDetailResponse
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    public string? Topic { get; set; }
    public SenderInfo CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<MessageResponse> Messages { get; set; } = [];

    /// <summary>
    /// Map thread + message đã order sẵn. Service vẫn phải build Sender info
    /// cho mỗi message và cho CreatedBy bằng <see cref="SenderInfoFactory"/> sau khi gọi.
    /// </summary>
    public static ConversationDetailResponse From(Conversation c, IEnumerable<Message> orderedMessages)
    {
        var list = orderedMessages.ToList();
        return new ConversationDetailResponse
        {
            Id = c.Id,
            ProjectWorkingId = c.ProjectWorkingId,
            Topic = c.Topic,
            CreatedBy = new SenderInfo { AccountId = c.CreatedBy, Role = c.CreatedByAccount.Role.ToString() },
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            // Service sẽ build Sender cho từng MessageResponse trước khi trả.
            Messages = list.Select(MessageResponse.From).ToList()
        };
    }
}
