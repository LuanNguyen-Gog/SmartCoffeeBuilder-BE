namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>File tài liệu/kỹ thuật (KHÔNG phải ảnh design).</summary>
public class Doc
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid DocTypeId { get; set; }
    public string FileUrl { get; set; } = null!;
    public string? FileName { get; set; }
    public string? Caption { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public DocType DocType { get; set; } = null!;
    public Account? UploadedByAccount { get; set; }
}
