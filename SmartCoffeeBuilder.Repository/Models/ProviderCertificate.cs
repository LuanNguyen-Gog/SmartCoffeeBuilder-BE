using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Giấy phép / chứng chỉ / giải thưởng của nhà cung cấp (review 1.1: "năng lực").
///
/// Trước đây năng lực chỉ có <c>constructor_profiles.license_no</c> — một chuỗi, không có bản
/// scan, không có hạn hiệu lực, và designer thì không có chỗ nào để khai chứng chỉ cả.
/// </summary>
public class ProviderCertificate
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>service_providers.id</c>, cascade theo hồ sơ.</summary>
    public Guid ServiceProviderProfileId { get; set; }

    public CertificateKind Kind { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Đơn vị cấp.</summary>
    public string? Issuer { get; set; }

    /// <summary>Số hiệu giấy phép / chứng chỉ.</summary>
    public string? CertificateNo { get; set; }

    public DateOnly? IssuedAt { get; set; }

    /// <summary>Ngày hết hiệu lực. null = không có hạn.</summary>
    public DateOnly? ExpiresAt { get; set; }

    /// <summary>ObjectName bản scan trên bucket GCS.</summary>
    public string? FileUrl { get; set; }

    /// <summary>
    /// Admin đã đối chiếu bản gốc chưa. KHÁC <c>service_providers.is_verified</c> (xác minh cả hồ
    /// sơ) — ở đây là xác minh TỪNG giấy tờ, provider tự khai thì mặc định false.
    /// </summary>
    public bool IsVerified { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
}
