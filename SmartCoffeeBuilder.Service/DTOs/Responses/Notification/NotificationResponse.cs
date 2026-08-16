namespace SmartCoffeeBuilder.Service.DTOs.Responses.Notification;

public class NotificationResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public bool IsRead { get; set; }
    /// <summary>Đã gửi email hay chưa (null = chưa gửi được, có thể resend).</summary>
    public DateTime? EmailSentAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static NotificationResponse From(SmartCoffeeBuilder.Repository.Models.Notification e) => new()
    {
        Id = e.Id,
        AccountId = e.AccountId,
        Type = e.Type,
        Title = e.Title,
        Content = e.Content,
        ReferenceType = e.ReferenceType,
        ReferenceId = e.ReferenceId,
        IsRead = e.IsRead,
        EmailSentAt = e.EmailSentAt,
        CreatedAt = e.CreatedAt
    };
}
