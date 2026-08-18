namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một thread trò chuyện trong engagement (<see cref="ProjectWorking"/>).
/// Cả owner và provider đều là "member" của engagement nên được nhắn
/// trong mọi thread thuộc engagement đó. Tin nhắn đính kèm qua <see cref="MessageAttachment"/>.
/// Phục vụ polling (FE định kỳ hỏi GET …/messages?sinceId=…).
/// </summary>
public class Conversation
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    /// <summary>Tên thread do người tạo đặt; trống thì tự sinh "Thread #N" của engagement.</summary>
    public string? Topic { get; set; }
    /// <summary>Account id của người tạo thread — chỉ người này được xoá thread.</summary>
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>Cập nhật mỗi khi có message mới — để sắp xếp "hoạt động gần nhất" và polling list thread.</summary>
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account CreatedByAccount { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
