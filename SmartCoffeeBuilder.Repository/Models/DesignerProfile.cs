namespace SmartCoffeeBuilder.Repository.Models;

public class DesignerProfile
{
    public long Id { get; set; }
    public long ProviderId { get; set; }
    public string Specialties { get; set; } = null!;
    public string SoftwareSkills { get; set; } = null!;
    public string DesignStyle { get; set; } = null!;
    public decimal? MinProjectBudget { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ServiceProvider Provider { get; set; } = null!;
}
