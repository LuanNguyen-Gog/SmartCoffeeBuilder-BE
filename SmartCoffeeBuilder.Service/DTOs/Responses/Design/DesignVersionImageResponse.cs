using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Design;

public class DesignVersionImageResponse
{
    public Guid Id { get; set; }
    public Guid DesignVersionId { get; set; }

    /// <summary>ObjectName trên bucket — giá trị COPY tại thời điểm snapshot (không phụ thuộc ảnh gốc).</summary>
    public string ImageUrl { get; set; } = null!;

    /// <summary>URL public tuyệt đối — FE dùng thẳng làm img src.</summary>
    public string? ViewUrl { get; set; }

    public string? Caption { get; set; }
    public Guid? UploadedBy { get; set; }

    /// <summary>CreatedAt của ảnh gốc — giữ lại cho lịch sử.</summary>
    public DateTime UploadedAt { get; set; }

    public static DesignVersionImageResponse From(DesignVersionImage i) => new()
    {
        Id = i.Id,
        DesignVersionId = i.DesignVersionId,
        ImageUrl = i.ImageUrl,
        ViewUrl = MediaUrl.Resolve(i.ImageUrl),
        Caption = i.Caption,
        UploadedBy = i.UploadedBy,
        UploadedAt = i.UploadedAt
    };
}