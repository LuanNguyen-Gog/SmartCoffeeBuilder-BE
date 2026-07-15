namespace SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProviderProfile;

public class UpdateServiceProviderProfileRequest
{
    public string? DisplayName { get; set; }

    /// <summary>individual | company</summary>
    public string? ProviderType { get; set; }

    /// <summary>designer | constructor | both</summary>
    public string? Capability { get; set; }

    public string? Bio { get; set; }
    public string? CompanyTaxCode { get; set; }
    public int? YearsExperience { get; set; }
    public string? PortfolioHeadline { get; set; }
    public bool? IsVerified { get; set; }
}
