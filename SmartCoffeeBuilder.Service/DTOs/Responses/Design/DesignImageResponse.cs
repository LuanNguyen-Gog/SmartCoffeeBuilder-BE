namespace SmartCoffeeBuilder.Service.DTOs.Responses.Design;

public class DesignImageResponse
{
    public long Id { get; set; }
    public long DesignId { get; set; }
    /// <summary>ObjectName trên bucket (ví dụ provider/5/2026/07/abc.png).</summary>
    public string ImageUrl { get; set; } = null!;
    /// <summary>URL public trên GCS ("https://storage.googleapis.com/{bucket}/{objectName}") — cố định, không hết hạn.</summary>
    public string? ViewUrl { get; set; }
    public string? Caption { get; set; }
    public long? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public static DesignImageResponse From(SmartCoffeeBuilder.Repository.Models.DesignImage e) => new()
    {
        Id = e.Id,
        DesignId = e.DesignId,
        ImageUrl = e.ImageUrl,
        Caption = e.Caption,
        UploadedBy = e.UploadedBy,
        CreatedAt = e.CreatedAt
    };
}
