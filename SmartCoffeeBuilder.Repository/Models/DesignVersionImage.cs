namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Bản copy của <see cref="DesignImage"/> tại thời điểm chụp <see cref="DesignVersion"/>.
/// <c>ImageUrl</c> là ObjectName COPY lúc snapshot — khi ảnh gốc bị xoá sau này (qua DesignService.RemoveFileAsync)
/// thì ảnh trong bản version vẫn còn truy cập được, chỉ <c>OriginalImageId</c> trở thành null (FK SET NULL).
/// </summary>
public class DesignVersionImage
{
    public long Id { get; set; }

    /// <summary>FK -> <c>design_versions.id</c>, cascade theo version.</summary>
    public long DesignVersionId { get; set; }

    /// <summary>
    /// FK mềm về <c>design_images.id</c> gốc. Nullable: nếu ảnh gốc bị xoá thì cột này set null
    /// nhưng bản snapshot vẫn giữ <see cref="ImageUrl"/>.
    /// </summary>
    public long? OriginalImageId { get; set; }

    /// <summary>ObjectName trên bucket, COPY tại thời điểm snapshot (không phụ thuộc ảnh gốc).</summary>
    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public long? UploadedBy { get; set; }

    /// <summary>CreatedAt của ảnh gốc — giữ lại cho lịch sử.</summary>
    public DateTime UploadedAt { get; set; }

    public DesignVersion DesignVersion { get; set; } = null!;
    public DesignImage? OriginalImage { get; set; }
    public Account? UploadedByAccount { get; set; }
}