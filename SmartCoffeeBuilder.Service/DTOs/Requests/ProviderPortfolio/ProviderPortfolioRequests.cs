using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProviderPortfolio;

/// <summary>
/// Thêm một dự án mẫu vào hồ sơ năng lực (review 1.1). Hồ sơ tự khai, không liên kết với
/// <c>projects</c> trong hệ thống — phần lớn công trình cũ diễn ra ngoài nền tảng.
/// </summary>
public class CreateProviderPortfolioRequest
{
    /// <summary>
    /// Hồ sơ provider nhận dự án mẫu này. Bỏ trống = hồ sơ của chính tài khoản đang đăng nhập
    /// (đường dùng thường ngày; điền tay chỉ có ý nghĩa với admin).
    /// </summary>
    public Guid? ServiceProviderProfileId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>design | construction | both — vai trò provider đảm nhận ở công trình đó.</summary>
    public string? Role { get; set; }

    [MaxLength(100)]
    public string? Style { get; set; }

    [MaxLength(255)]
    public string? Location { get; set; }

    public decimal? AreaM2 { get; set; }
    public decimal? ContractValue { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public int? DurationDays { get; set; }

    /// <summary>Link YouTube hoặc ObjectName video trên bucket.</summary>
    [MaxLength(500)]
    public string? VideoUrl { get; set; }

    /// <summary>ObjectName ảnh bìa — upload qua /api/files trước rồi gửi giá trị trả về.</summary>
    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }

    public bool IsFeatured { get; set; }
    public int? SortOrder { get; set; }

    /// <summary>Bộ ảnh công trình, khai luôn lúc tạo (tuỳ chọn).</summary>
    public List<ProviderPortfolioImageRequest>? Images { get; set; }
}

/// <summary>Cập nhật dự án mẫu. Field null = giữ nguyên.</summary>
public class UpdateProviderPortfolioRequest
{
    [MaxLength(255)]
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Role { get; set; }
    [MaxLength(100)]
    public string? Style { get; set; }
    [MaxLength(255)]
    public string? Location { get; set; }
    public decimal? AreaM2 { get; set; }
    public decimal? ContractValue { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public int? DurationDays { get; set; }
    [MaxLength(500)]
    public string? VideoUrl { get; set; }
    [MaxLength(500)]
    public string? CoverImageUrl { get; set; }
    public bool? IsFeatured { get; set; }
    public int? SortOrder { get; set; }
}

/// <summary>Một ảnh của dự án mẫu.</summary>
public class ProviderPortfolioImageRequest
{
    /// <summary>ObjectName trên bucket — upload qua /api/files trước.</summary>
    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }
    public int? SortOrder { get; set; }
}
