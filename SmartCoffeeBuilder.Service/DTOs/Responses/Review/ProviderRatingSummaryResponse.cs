namespace SmartCoffeeBuilder.Service.DTOs.Responses.Review;

/// <summary>Tổng hợp rating của một provider — dùng cho trang profile provider.</summary>
public class ProviderRatingSummaryResponse
{
    public long ServiceProviderProfileId { get; set; }
    public int ReviewCount { get; set; }
    /// <summary>Trung bình OverallRating, làm tròn 2 chữ số. Null nếu chưa có review.</summary>
    public decimal? AverageRating { get; set; }
    /// <summary>Điểm trung bình theo từng tiêu chí (dimension → average).</summary>
    public Dictionary<string, decimal> DimensionAverages { get; set; } = new();
}
