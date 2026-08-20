namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một ảnh của dự án mẫu. Tách bảng vì một công trình cần nhiều góc chụp (mặt tiền, quầy, khu ngồi)
/// — nhét mảng url vào một cột thì không sắp thứ tự và không ghi chú từng ảnh được.
/// Lưu ObjectName trên bucket GCS như mọi chỗ khác trong hệ thống (xem IFileStorageService).
/// </summary>
public class ProviderPortfolioImage
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>provider_portfolios.id</c>, cascade theo dự án mẫu.</summary>
    public Guid ProviderPortfolioId { get; set; }

    /// <summary>ObjectName trên bucket (FE hiển thị bằng URL public do BE resolve).</summary>
    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    /// <summary>Thứ tự hiển thị trong bộ ảnh.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public ProviderPortfolio ProviderPortfolio { get; set; } = null!;
}
