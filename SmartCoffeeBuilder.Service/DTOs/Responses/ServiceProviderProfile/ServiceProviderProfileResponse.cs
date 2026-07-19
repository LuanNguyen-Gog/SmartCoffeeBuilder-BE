using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ServiceProviderProfile;

public class ServiceProviderProfileResponse
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string ProviderType { get; set; } = null!;
    public string Capability { get; set; } = null!;
    public string? Bio { get; set; }
    public string? CompanyTaxCode { get; set; }
    public int? YearsExperience { get; set; }
    public string? PortfolioHeadline { get; set; }
    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ServiceProviderProfileResponse From(SmartCoffeeBuilder.Repository.Models.ServiceProviderProfile p) => new()
    {
        Id = p.Id,
        AccountId = p.AccountId,
        DisplayName = p.DisplayName,
        ProviderType = p.ProviderType.ToString(),
        Capability = p.Capability.ToString(),
        Bio = p.Bio,
        CompanyTaxCode = p.CompanyTaxCode,
        YearsExperience = p.YearsExperience,
        PortfolioHeadline = p.PortfolioHeadline,
        IsVerified = p.IsVerified,
        AvgRating = p.AvgRating,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
