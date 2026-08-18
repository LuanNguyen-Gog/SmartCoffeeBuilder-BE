namespace SmartCoffeeBuilder.Repository.Models;

public class DesignImage
{
    public Guid Id { get; set; }
    public Guid DesignId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Caption { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Design Design { get; set; } = null!;
    public Account? UploadedByAccount { get; set; }
}
