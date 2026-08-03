using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Responses.Admin;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>Nghiệp vụ dành riêng cho admin: thống kê nền tảng, thống kê tài khoản, báo cáo doanh thu.</summary>
public interface IAdminService
{
    /// <summary>Bảng tổng quan dashboard (tài khoản, dự án, marketplace, hợp đồng, doanh thu).</summary>
    Task<PlatformOverviewResponse> GetOverviewAsync();

    /// <summary>Thống kê tài khoản theo vai trò/trạng thái.</summary>
    Task<AccountStatisticsResponse> GetAccountStatisticsAsync();

    /// <summary>Báo cáo doanh thu (giao dịch payOS 'paid') trong khoảng thời gian, chia theo day/month.</summary>
    Task<RevenueReportResponse> GetRevenueReportAsync(DateTime? from, DateTime? to, string groupBy = "month");

    /// <summary>Danh sách giao dịch payOS (drill-down), lọc theo status/purpose/khoảng thời gian.</summary>
    Task<PaginationResponse<RevenueTransactionResponse>> GetRevenueTransactionsAsync(
        int pageNumber = 1, int pageSize = 20,
        string? status = null, string? purpose = null, DateTime? from = null, DateTime? to = null);
}
