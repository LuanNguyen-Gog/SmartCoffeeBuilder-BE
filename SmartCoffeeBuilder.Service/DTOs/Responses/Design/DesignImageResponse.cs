namespace SmartCoffeeBuilder.Service.DTOs.Responses.Design;

public class DesignImageResponse
{
    public long Id { get; set; }
    public long DesignId { get; set; }
    public string ImageUrl { get; set; } = null!;
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
