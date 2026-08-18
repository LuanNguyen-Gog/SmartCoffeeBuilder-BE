namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// File/ảnh đính kèm một message — nhiều attachment trên một message.
/// ObjectName lưu trong DB; URL public resolve qua <c>MediaUrl</c>
/// ("https://storage.googleapis.com/{bucket}/{objectName}").
/// Folder trên bucket: "messages".
/// </summary>
public class MessageAttachment
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    /// <summary>ObjectName trên GCS — giá trị thật lưu DB.</summary>
    public string Url { get; set; } = null!;
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Message Message { get; set; } = null!;
}
