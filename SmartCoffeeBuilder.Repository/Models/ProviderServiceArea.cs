namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Khu vực nhà cung cấp nhận việc (review 1.1: phần "năng lực" của hồ sơ).
///
/// Đây là thông tin LỌC thật sự chứ không phải trang trí: chủ quán ở Đà Nẵng không có lý do gì để
/// thấy nhà thầu chỉ chạy Hà Nội. Tách bảng vì một provider phục vụ nhiều tỉnh.
/// </summary>
public class ProviderServiceArea
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>service_providers.id</c>, cascade theo hồ sơ.</summary>
    public Guid ServiceProviderProfileId { get; set; }

    /// <summary>Tỉnh / thành phố. UNIQUE cùng provider + quận huyện.</summary>
    public string Province { get; set; } = null!;

    /// <summary>Quận / huyện. null = nhận toàn tỉnh.</summary>
    public string? District { get; set; }

    public string? Note { get; set; }

    /// <summary>Thứ tự hiển thị.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
}
