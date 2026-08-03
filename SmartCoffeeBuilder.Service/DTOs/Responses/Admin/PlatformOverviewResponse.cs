namespace SmartCoffeeBuilder.Service.DTOs.Responses.Admin;

/// <summary>Bảng tổng quan cho dashboard admin — số liệu toàn nền tảng tại thời điểm gọi.</summary>
public class PlatformOverviewResponse
{
    public AccountStatisticsResponse Accounts { get; set; } = new();
    public CountByStatus Projects { get; set; } = new();
    public CountByStatus Posts { get; set; } = new();
    public CountByStatus Applications { get; set; } = new();
    /// <summary>Engagement (project_working) theo trạng thái quan hệ hợp tác.</summary>
    public CountByStatus Engagements { get; set; } = new();
    public CountByStatus Contracts { get; set; } = new();
    public int ActiveSubscriptions { get; set; }
    public RevenueSummary Revenue { get; set; } = new();
}

public class RevenueSummary
{
    public string Currency { get; set; } = "VND";
    /// <summary>Tổng doanh thu 'paid' toàn thời gian.</summary>
    public decimal Total { get; set; }
    /// <summary>Doanh thu 'paid' từ đầu tháng hiện tại (UTC).</summary>
    public decimal ThisMonth { get; set; }
    public int PaidTransactions { get; set; }
}
