using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ServiceProvider
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string DisplayName { get; set; } = null!;
    public ProviderType ProviderType { get; set; }
    public Capability Capability { get; set; }
    public string? Bio { get; set; }
    public string? CompanyTaxCode { get; set; }
    public int? YearsExperience { get; set; }
    public string? PortfolioHeadline { get; set; }
    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Account Account { get; set; } = null!;
    public DesignerProfile? DesignerProfile { get; set; }
    public ConstructorProfile? ConstructorProfile { get; set; }
    public ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();
    public ICollection<ProjectProvider> ProjectProviders { get; set; } = new List<ProjectProvider>();
}
