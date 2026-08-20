using SmartCoffeeBuilder.Service.Utils;
using Entities = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProviderBrand;

/// <summary>
/// Toàn bộ phần THƯƠNG HIỆU + NĂNG LỰC của một hồ sơ nhà cung cấp (review 1.1), gộp một lần gọi:
/// nhận diện thương hiệu, kênh mạng xã hội, khu vực phục vụ, giấy phép/chứng chỉ.
///
/// Dự án mẫu KHÔNG nằm ở đây — nó phân trang riêng qua <c>/api/provider-portfolios</c> vì một
/// provider lâu năm có thể có hàng chục công trình.
/// </summary>
public class ProviderBrandResponse
{
    public Guid ServiceProviderProfileId { get; set; }
    public string DisplayName { get; set; } = null!;

    public string? LogoUrl { get; set; }

    /// <summary>URL public của logo — FE dùng thẳng làm img src.</summary>
    public string? LogoViewUrl { get; set; }

    public string? CoverImageUrl { get; set; }
    public string? CoverImageViewUrl { get; set; }

    public string? IntroVideoUrl { get; set; }

    /// <summary>Link ngoài giữ nguyên; ObjectName trên bucket được resolve thành URL public.</summary>
    public string? IntroVideoViewUrl { get; set; }

    public string? Website { get; set; }
    public string? BrandStory { get; set; }
    public string? CompanyAddress { get; set; }
    public int? FoundedYear { get; set; }
    public int? EmployeeCount { get; set; }

    /// <summary>Năm kinh nghiệm tự khai — giữ lại ở đây để FE vẽ block "năng lực" một chỗ.</summary>
    public int? YearsExperience { get; set; }

    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }
    public int ReviewCount { get; set; }

    public List<ProviderSocialLinkResponse> SocialLinks { get; set; } = new();
    public List<ProviderServiceAreaResponse> ServiceAreas { get; set; } = new();
    public List<ProviderCertificateResponse> Certificates { get; set; } = new();

    public static ProviderBrandResponse From(Entities.ServiceProviderProfile p) => new()
    {
        ServiceProviderProfileId = p.Id,
        DisplayName = p.DisplayName,
        LogoUrl = p.LogoUrl,
        LogoViewUrl = MediaUrl.Resolve(p.LogoUrl),
        CoverImageUrl = p.CoverImageUrl,
        CoverImageViewUrl = MediaUrl.Resolve(p.CoverImageUrl),
        IntroVideoUrl = p.IntroVideoUrl,
        IntroVideoViewUrl = MediaUrl.Resolve(p.IntroVideoUrl),
        Website = p.Website,
        BrandStory = p.BrandStory,
        CompanyAddress = p.CompanyAddress,
        FoundedYear = p.FoundedYear,
        EmployeeCount = p.EmployeeCount,
        YearsExperience = p.YearsExperience,
        IsVerified = p.IsVerified,
        AvgRating = p.AvgRating,
        ReviewCount = p.ReviewCount,
        SocialLinks = (p.SocialLinks ?? new List<Entities.ProviderSocialLink>())
            .OrderBy(x => x.SortOrder).Select(ProviderSocialLinkResponse.From).ToList(),
        ServiceAreas = (p.ServiceAreas ?? new List<Entities.ProviderServiceArea>())
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Province)
            .Select(ProviderServiceAreaResponse.From).ToList(),
        Certificates = (p.Certificates ?? new List<Entities.ProviderCertificate>())
            .OrderBy(x => x.SortOrder).Select(ProviderCertificateResponse.From).ToList()
    };
}

public class ProviderSocialLinkResponse
{
    public Guid Id { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string Platform { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? Label { get; set; }
    public int SortOrder { get; set; }

    public static ProviderSocialLinkResponse From(Entities.ProviderSocialLink e) => new()
    {
        Id = e.Id,
        ServiceProviderProfileId = e.ServiceProviderProfileId,
        Platform = e.Platform.ToString(),
        Url = e.Url,
        Label = e.Label,
        SortOrder = e.SortOrder
    };
}

public class ProviderServiceAreaResponse
{
    public Guid Id { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string Province { get; set; } = null!;
    public string? District { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }

    public static ProviderServiceAreaResponse From(Entities.ProviderServiceArea e) => new()
    {
        Id = e.Id,
        ServiceProviderProfileId = e.ServiceProviderProfileId,
        Province = e.Province,
        District = e.District,
        Note = e.Note,
        SortOrder = e.SortOrder
    };
}

public class ProviderCertificateResponse
{
    public Guid Id { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string Kind { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Issuer { get; set; }
    public string? CertificateNo { get; set; }
    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }
    public string? FileUrl { get; set; }
    public string? FileViewUrl { get; set; }

    /// <summary>Admin đã đối chiếu bản gốc chưa — provider tự khai thì luôn false.</summary>
    public bool IsVerified { get; set; }

    /// <summary>Chứng chỉ đã quá hạn hay chưa (suy từ <see cref="ExpiresAt"/>). null = không có hạn.</summary>
    public bool? IsExpired { get; set; }

    public int SortOrder { get; set; }

    public static ProviderCertificateResponse From(Entities.ProviderCertificate e) => new()
    {
        Id = e.Id,
        ServiceProviderProfileId = e.ServiceProviderProfileId,
        Kind = e.Kind.ToString(),
        Name = e.Name,
        Issuer = e.Issuer,
        CertificateNo = e.CertificateNo,
        IssuedAt = e.IssuedAt,
        ExpiresAt = e.ExpiresAt,
        FileUrl = e.FileUrl,
        FileViewUrl = MediaUrl.Resolve(e.FileUrl),
        IsVerified = e.IsVerified,
        IsExpired = e.ExpiresAt is DateOnly exp
            ? exp < DateOnly.FromDateTime(DateTime.UtcNow)
            : null,
        SortOrder = e.SortOrder
    };
}
