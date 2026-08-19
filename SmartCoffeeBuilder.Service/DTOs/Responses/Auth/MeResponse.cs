namespace SmartCoffeeBuilder.Service.DTOs.Responses.Auth;

public class MeResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ShopOwner info (populated if role == owner)
    public ShopOwnerInfo? ShopOwner { get; set; }

    // ServiceProvider info (populated if role == provider)
    public ServiceProviderInfo? ServiceProvider { get; set; }
}

public class ShopOwnerInfo
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = null!;
    public string ShopName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
}

public class ServiceProviderInfo
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public string ProviderType { get; set; } = null!; // Designer | Constructor
    public string Capability { get; set; } = null!;   // Design | Construction | DesignAndConstruction
    public string? Bio { get; set; }
    public string? CompanyTaxCode { get; set; }
    public int? YearsExperience { get; set; }
    public string? PortfolioHeadline { get; set; }
    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Designer-specific fields (populated if ProviderType == Designer)
    public DesignerInfo? Designer { get; set; }

    // Constructor-specific fields (populated if ProviderType == Constructor)
    public ConstructorInfo? Constructor { get; set; }
}

public class DesignerInfo
{
    public string Specialties { get; set; } = null!;
    public string SoftwareSkills { get; set; } = null!;
    public string DesignStyle { get; set; } = null!;
    public decimal? MinProjectBudget { get; set; }
}

public class ConstructorInfo
{
    public string LicenseNo { get; set; } = null!;
    public int TeamSize { get; set; }
    public string Equipment { get; set; } = null!;
    public decimal? MaxProjectValue { get; set; }
    public string? WarrantyPolicy { get; set; }
}
