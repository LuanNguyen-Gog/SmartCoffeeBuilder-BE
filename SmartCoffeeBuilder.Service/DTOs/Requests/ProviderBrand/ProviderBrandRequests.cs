using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProviderBrand;

/// <summary>
/// Cập nhật thông tin THƯƠNG HIỆU của hồ sơ nhà cung cấp (review 1.1). Field null = giữ nguyên.
///
/// Tách khỏi <c>UpdateServiceProviderProfileRequest</c> có chủ đích: endpoint đó hiện chỉ có role
/// gate nên provider nào cũng gọi được lên hồ sơ của provider khác. Đường này bắt buộc đi qua
/// ownership check theo account.
/// </summary>
public class UpdateProviderBrandRequest
{
    /// <summary>ObjectName logo — upload qua /api/files trước rồi gửi giá trị trả về.</summary>
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    /// <summary>Link YouTube hoặc ObjectName video giới thiệu năng lực.</summary>
    [MaxLength(500)]
    public string? IntroVideoUrl { get; set; }

    [MaxLength(500)]
    public string? Website { get; set; }

    public string? BrandStory { get; set; }

    [MaxLength(500)]
    public string? CompanyAddress { get; set; }

    public int? FoundedYear { get; set; }
    public int? EmployeeCount { get; set; }
}

/// <summary>Một kênh thương hiệu. Mỗi nền tảng chỉ khai được MỘT dòng cho một provider.</summary>
public class ProviderSocialLinkRequest
{
    /// <summary>facebook | instagram | tiktok | youtube | linkedin | zalo | website | other.</summary>
    [Required]
    public string Platform { get; set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Url { get; set; } = null!;

    [MaxLength(150)]
    public string? Label { get; set; }

    public int? SortOrder { get; set; }
}

/// <summary>Một khu vực nhận việc. Bỏ trống <c>district</c> = nhận toàn tỉnh.</summary>
public class ProviderServiceAreaRequest
{
    [Required]
    [MaxLength(100)]
    public string Province { get; set; } = null!;

    [MaxLength(100)]
    public string? District { get; set; }

    public string? Note { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>Một giấy phép / chứng chỉ / giải thưởng.</summary>
public class ProviderCertificateRequest
{
    /// <summary>license | certificate | award | membership | other.</summary>
    [Required]
    public string Kind { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = null!;

    [MaxLength(255)]
    public string? Issuer { get; set; }

    [MaxLength(100)]
    public string? CertificateNo { get; set; }

    public DateOnly? IssuedAt { get; set; }
    public DateOnly? ExpiresAt { get; set; }

    /// <summary>ObjectName bản scan — upload qua /api/files trước.</summary>
    [MaxLength(500)]
    public string? FileUrl { get; set; }

    public int? SortOrder { get; set; }
}
