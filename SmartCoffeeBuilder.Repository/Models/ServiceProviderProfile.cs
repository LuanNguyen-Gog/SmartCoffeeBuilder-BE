using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ServiceProviderProfile
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string DisplayName { get; set; } = null!;
    public ProviderType ProviderType { get; set; }
    public Capability Capability { get; set; }
    public string? Bio { get; set; }
    public string? CompanyTaxCode { get; set; }
    public int? YearsExperience { get; set; }
    public string? PortfolioHeadline { get; set; }
    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }

    /// <summary>
    /// Số review đã nhận. Bản sao cho tiện đọc — nguồn sự thật là bảng <c>reviews</c>;
    /// <c>ReviewService.SyncProviderRatingAsync</c> giữ đồng bộ cùng lúc với <see cref="AvgRating"/>.
    /// Có nó thì danh sách provider hiện được "4.8 (23 đánh giá)" mà không phải join đếm mỗi lần.
    /// </summary>
    public int ReviewCount { get; set; }

    // ── Thương hiệu (review 1.1) ────────────────────────────────────────────────────

    /// <summary>ObjectName logo trên bucket GCS.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>ObjectName ảnh bìa hồ sơ.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Video giới thiệu năng lực (link YouTube hoặc ObjectName trên bucket).</summary>
    public string? IntroVideoUrl { get; set; }

    public string? Website { get; set; }

    /// <summary>Câu chuyện thương hiệu — dài hơn và khác mục đích với <see cref="Bio"/>.</summary>
    public string? BrandStory { get; set; }

    /// <summary>Địa chỉ văn phòng / xưởng.</summary>
    public string? CompanyAddress { get; set; }

    /// <summary>Năm thành lập — khác <see cref="YearsExperience"/> do cá nhân tự khai.</summary>
    public int? FoundedYear { get; set; }

    /// <summary>Quy mô nhân sự của cả doanh nghiệp (constructor_profiles.team_size là đội thi công).</summary>
    public int? EmployeeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Account Account { get; set; } = null!;
    public DesignerProfile? DesignerProfile { get; set; }
    public ConstructorProfile? ConstructorProfile { get; set; }
    public ICollection<Apply> Applies { get; set; } = new List<Apply>();
    public ICollection<ProjectWorking> ProjectWorkings { get; set; } = new List<ProjectWorking>();

    /// <summary>Dự án mẫu trong hồ sơ năng lực (review 1.1).</summary>
    public ICollection<ProviderPortfolio> Portfolios { get; set; } = new List<ProviderPortfolio>();

    /// <summary>Kênh thương hiệu: fanpage, Instagram, website… (review 1.1).</summary>
    public ICollection<ProviderSocialLink> SocialLinks { get; set; } = new List<ProviderSocialLink>();

    /// <summary>Khu vực nhận việc.</summary>
    public ICollection<ProviderServiceArea> ServiceAreas { get; set; } = new List<ProviderServiceArea>();

    /// <summary>Giấy phép / chứng chỉ / giải thưởng.</summary>
    public ICollection<ProviderCertificate> Certificates { get; set; } = new List<ProviderCertificate>();
}
