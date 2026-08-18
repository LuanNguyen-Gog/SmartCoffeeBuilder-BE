namespace SmartCoffeeBuilder.Repository.Models;

public class ConstructorProfile
{
    public Guid Id { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string LicenseNo { get; set; } = null!;
    public int TeamSize { get; set; }
    public string Equipment { get; set; } = null!;
    public decimal? MaxProjectValue { get; set; }
    public string? WarrantyPolicy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
}
