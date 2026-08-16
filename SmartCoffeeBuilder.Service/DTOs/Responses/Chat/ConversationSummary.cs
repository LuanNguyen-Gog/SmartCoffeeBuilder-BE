using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Chat;

/// <summary>Một dòng trong list thread — service build LastMessage bằng cách truy vấn riêng.</summary>
public class ConversationSummary
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }

    /// <summary>Topic có thể đã được service tự đặt "Thread #N" nếu FE gửi rỗng.</summary>
    public string? Topic { get; set; }

    public SenderInfo CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    /// <summary>Cập nhật mỗi khi có message mới — sort "hoạt động gần nhất".</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Tin nhắn cuối của thread — FE hiển thị preview. Null nếu thread chưa có message.</summary>
    public MessageResponse? LastMessage { get; set; }

    /// <summary>Số tin chưa đọc — luôn 0 ở v1, đếm sau khi có bảng read receipts.</summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// Map field cơ bản (không fill LastMessage, CreatedBy). Service load thêm:
    /// - CreatedBy = <c>SenderInfoFactory.BuildAsync(conversation.CreatedByAccount, ...)</c>
    /// - LastMessage = truy vấn message mới nhất qua <c>IGenericRepository&lt;Message&gt;</c>
    /// </summary>
    public static ConversationSummary From(Conversation c) => new()
    {
        Id = c.Id,
        ProjectWorkingId = c.ProjectWorkingId,
        Topic = c.Topic,
        // CreatedBy gán sau ở service.
        CreatedBy = new SenderInfo { AccountId = c.CreatedBy, Role = c.CreatedByAccount.Role.ToString() },
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        LastMessage = null,
        UnreadCount = 0
    };
}
