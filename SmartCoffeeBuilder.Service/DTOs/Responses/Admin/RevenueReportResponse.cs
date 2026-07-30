namespace SmartCoffeeBuilder.Service.DTOs.Responses.Admin;

/// <summary>
/// Báo cáo doanh thu nền tảng — chỉ tính giao dịch payOS đã 'paid'.
/// Doanh thu = phí subscription + phí post_boost.
/// </summary>
public class RevenueReportResponse
{
    /// <summary>Mốc lọc (UTC); null = không giới hạn.</summary>
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    /// <summary>"day" hoặc "month" — độ chia của chuỗi thời gian Series.</summary>
    public string GroupBy { get; set; } = "month";
    public string Currency { get; set; } = "VND";

    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }

    /// <summary>Phân rã theo mục đích (subscription / post_boost).</summary>
    public List<RevenueByPurpose> ByPurpose { get; set; } = [];
    /// <summary>Chuỗi thời gian doanh thu theo kỳ (đã sắp xếp tăng dần).</summary>
    public List<RevenuePeriodPoint> Series { get; set; } = [];
}

public class RevenueByPurpose
{
    public string Purpose { get; set; } = null!;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}

public class RevenuePeriodPoint
{
    /// <summary>Nhãn kỳ: "yyyy-MM-dd" (day) hoặc "yyyy-MM" (month).</summary>
    public string Period { get; set; } = null!;
    public decimal Amount { get; set; }
    public int Count { get; set; }
}
