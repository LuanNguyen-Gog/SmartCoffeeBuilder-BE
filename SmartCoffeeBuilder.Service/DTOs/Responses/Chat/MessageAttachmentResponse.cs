using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Chat;

/// <summary>File/ảnh đính kèm trên message — phải resolve URL public ở mọi response (không chỉ lúc upload).</summary>
public class MessageAttachmentResponse
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }

    /// <summary>ObjectName trên bucket — giá trị thật lưu trong DB.</summary>
    public string Url { get; set; } = null!;

    /// <summary>URL public tuyệt đối ("https://storage.googleapis.com/{bucket}/{objectName}") — FE dùng thẳng.</summary>
    public string? ViewUrl { get; set; }

    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }

    public static MessageAttachmentResponse From(MessageAttachment a) => new()
    {
        Id = a.Id,
        MessageId = a.MessageId,
        Url = a.Url,
        ViewUrl = MediaUrl.Resolve(a.Url),
        FileName = a.FileName,
        ContentType = a.ContentType,
        SizeBytes = a.SizeBytes,
        CreatedAt = a.CreatedAt
    };
}
