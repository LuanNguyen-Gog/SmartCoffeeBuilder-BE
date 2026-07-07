namespace SmartCoffeeBuilder.Repository.Models;

public class DesignImage
{
    public long Id { get; set; }
    public long DesignId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Caption { get; set; }
    public long? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Design Design { get; set; } = null!;
    public Account? UploadedByAccount { get; set; }
}
