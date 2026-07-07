namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>File tài liệu/kỹ thuật (KHÔNG phải ảnh design).</summary>
public class Doc
{
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public long DocTypeId { get; set; }
    public string FileUrl { get; set; } = null!;
    public string? FileName { get; set; }
    public string? Caption { get; set; }
    public long? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public ProjectProvider ProjectProvider { get; set; } = null!;
    public DocType DocType { get; set; } = null!;
    public Account? UploadedByAccount { get; set; }
}
