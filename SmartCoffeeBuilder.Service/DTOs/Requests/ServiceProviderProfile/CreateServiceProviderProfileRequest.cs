using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProviderProfile;

public class CreateServiceProviderProfileRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public string DisplayName { get; set; } = null!;

    /// <summary>individual | company</summary>
    [Required]
    public string ProviderType { get; set; } = null!;

    /// <summary>designer | constructor | both</summary>
    [Required]
    public string Capability { get; set; } = null!;

    public string? Bio { get; set; }
    public string? CompanyTaxCode { get; set; }
    public int? YearsExperience { get; set; }
    public string? PortfolioHeadline { get; set; }
}
