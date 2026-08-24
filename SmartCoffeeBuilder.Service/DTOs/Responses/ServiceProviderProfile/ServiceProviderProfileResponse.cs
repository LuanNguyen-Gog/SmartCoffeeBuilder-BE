using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ServiceProviderProfile;

public class ServiceProviderProfileResponse
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string ProviderType { get; set; } = null!;
    public string Capability { get; set; } = null!;
    public string? Bio { get; set; }
    public string? CompanyTaxCode { get; set; }
    public int? YearsExperience { get; set; }
    public string? PortfolioHeadline { get; set; }
    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }

    /// <summary>Số đánh giá đã nhận — đồng bộ cùng AvgRating bởi ReviewService (review 1.1).</summary>
    public int ReviewCount { get; set; }

    // ── Thương hiệu (review 1.1) ────────────────────────────────────────────
    public string? LogoUrl { get; set; }
    public string? LogoViewUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CoverImageViewUrl { get; set; }
    public string? IntroVideoUrl { get; set; }
    public string? Website { get; set; }
    public string? BrandStory { get; set; }
    public string? CompanyAddress { get; set; }
    public int? FoundedYear { get; set; }
    public int? EmployeeCount { get; set; }
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
        ReviewCount = p.ReviewCount,
        LogoUrl = p.LogoUrl,
        LogoViewUrl = MediaUrl.Resolve(p.LogoUrl),
        CoverImageUrl = p.CoverImageUrl,
        CoverImageViewUrl = MediaUrl.Resolve(p.CoverImageUrl),
        IntroVideoUrl = p.IntroVideoUrl,
        Website = p.Website,
        BrandStory = p.BrandStory,
        CompanyAddress = p.CompanyAddress,
        FoundedYear = p.FoundedYear,
        EmployeeCount = p.EmployeeCount,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
