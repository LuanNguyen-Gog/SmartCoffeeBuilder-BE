using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Responses.Admin;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Thống kê + báo cáo cho admin. Chỉ đọc — tổng hợp số liệu từ nhiều bảng.
/// Doanh thu tính trên payment_transaction có status 'paid' (phí subscription + post_boost).
/// </summary>
public class AdminService : IAdminService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;

    public AdminService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private static DateTime CurrentMonthStartUtc()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public async Task<AccountStatisticsResponse> GetAccountStatisticsAsync()
    {
        // Tài khoản là bảng nhỏ → materialize các cột cần rồi tổng hợp nhiều chiều trong bộ nhớ.
        var rows = await _unitOfWork.GetRepository<Account>()
            .GetQueryable(a => a.DeletedAt == null)
            .Select(a => new { a.Role, a.Status, Verified = a.EmailVerifiedAt != null, a.CreatedAt })
            .ToListAsync();

        var monthStart = CurrentMonthStartUtc();

        return new AccountStatisticsResponse
        {
            Total = rows.Count,
            Owners = rows.Count(r => r.Role == AccountRole.owner),
            Providers = rows.Count(r => r.Role == AccountRole.provider),
            Admins = rows.Count(r => r.Role == AccountRole.admin),
            Active = rows.Count(r => r.Status == AccountStatus.active),
            Inactive = rows.Count(r => r.Status == AccountStatus.inactive),
            Banned = rows.Count(r => r.Status == AccountStatus.banned),
            Pending = rows.Count(r => r.Status == AccountStatus.pending),
            EmailVerified = rows.Count(r => r.Verified),
            NewThisMonth = rows.Count(r => r.CreatedAt >= monthStart)
        };
    }

    public async Task<PlatformOverviewResponse> GetOverviewAsync()
    {
        var accounts = await GetAccountStatisticsAsync();

        var projects = await ToCountByStatusAsync(_unitOfWork.GetRepository<ProjectShopOwner>()
            .GetQueryable(p => p.DeletedAt == null).Select(p => p.Status));
        var posts = await ToCountByStatusAsync(_unitOfWork.GetRepository<Post>()
            .GetQueryable().Select(p => p.Status));
        var applications = await ToCountByStatusAsync(_unitOfWork.GetRepository<Apply>()
            .GetQueryable().Select(a => a.Status));
        var engagements = await ToCountByStatusAsync(_unitOfWork.GetRepository<ProjectWorking>()
            .GetQueryable().Select(w => w.Status));
        var contracts = await ToCountByStatusAsync(_unitOfWork.GetRepository<Contract>()
            .GetQueryable().Select(c => c.Status));

        var activeSubs = await _unitOfWork.GetRepository<Subscription>()
            .CountAsync(s => s.Status == SubscriptionStatus.active);

        var paidRows = await _unitOfWork.GetRepository<PaymentTransaction>()
            .GetQueryable(t => t.Status == PaymentTransactionStatus.paid)
            .Select(t => new { t.Amount, t.CreatedAt })
            .ToListAsync();

        var monthStart = CurrentMonthStartUtc();

        return new PlatformOverviewResponse
        {
            Accounts = accounts,
            Projects = projects,
            Posts = posts,
            Applications = applications,
            Engagements = engagements,
            Contracts = contracts,
            ActiveSubscriptions = activeSubs,
            Revenue = new RevenueSummary
            {
                Total = paidRows.Sum(r => r.Amount),
                ThisMonth = paidRows.Where(r => r.CreatedAt >= monthStart).Sum(r => r.Amount),
                PaidTransactions = paidRows.Count
            }
        };
    }

    public async Task<RevenueReportResponse> GetRevenueReportAsync(DateTime? from, DateTime? to, string groupBy = "month")
    {
        groupBy = string.IsNullOrWhiteSpace(groupBy) ? "month" : groupBy.Trim().ToLowerInvariant();
        if (groupBy != "day" && groupBy != "month")
            throw new ArgumentException("groupBy chỉ nhận 'day' hoặc 'month'.");

        var fromUtc = ToUtc(from);
        var toUtc = ToUtc(to);

        var rows = await _unitOfWork.GetRepository<PaymentTransaction>()
            .GetQueryable(t => t.Status == PaymentTransactionStatus.paid
                && (fromUtc == null || t.CreatedAt >= fromUtc)
                && (toUtc == null || t.CreatedAt <= toUtc))
            .Select(t => new { t.Amount, t.Purpose, t.CreatedAt })
            .ToListAsync();

        var fmt = groupBy == "day" ? "yyyy-MM-dd" : "yyyy-MM";

        return new RevenueReportResponse
        {
            From = fromUtc,
            To = toUtc,
            GroupBy = groupBy,
            TotalRevenue = rows.Sum(r => r.Amount),
            TransactionCount = rows.Count,
            ByPurpose = rows
                .GroupBy(r => r.Purpose)
                .Select(g => new RevenueByPurpose { Purpose = g.Key.ToString(), Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderByDescending(x => x.Amount)
                .ToList(),
            Series = rows
                .GroupBy(r => r.CreatedAt.ToString(fmt, CultureInfo.InvariantCulture))
                .Select(g => new RevenuePeriodPoint { Period = g.Key, Amount = g.Sum(x => x.Amount), Count = g.Count() })
                .OrderBy(x => x.Period)
                .ToList()
        };
    }

    public async Task<PaginationResponse<RevenueTransactionResponse>> GetRevenueTransactionsAsync(
        int pageNumber = 1, int pageSize = 20,
        string? status = null, string? purpose = null, DateTime? from = null, DateTime? to = null)
    {
        PaymentTransactionStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PaymentTransactionStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, paid, cancelled, failed.");
            statusFilter = parsed;
        }

        PaymentPurpose? purposeFilter = null;
        if (!string.IsNullOrWhiteSpace(purpose))
        {
            if (!Enum.TryParse<PaymentPurpose>(purpose, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Purpose '{purpose}' không hợp lệ. Cho phép: subscription, post_boost.");
            purposeFilter = parsed;
        }

        var fromUtc = ToUtc(from);
        var toUtc = ToUtc(to);

        var paged = await _unitOfWork.GetRepository<PaymentTransaction>()
            .GetQueryable(t =>
                (statusFilter == null || t.Status == statusFilter)
                && (purposeFilter == null || t.Purpose == purposeFilter)
                && (fromUtc == null || t.CreatedAt >= fromUtc)
                && (toUtc == null || t.CreatedAt <= toUtc))
            .OrderByDescending(t => t.CreatedAt)
            .ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<RevenueTransactionResponse>(
            paged.Items.Select(RevenueTransactionResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    // ──────────────────────────────── Helpers ────────────────────────────────

    /// <summary>Timestamptz của Postgres yêu cầu DateTime Kind=Utc — coi input là UTC.</summary>
    private static DateTime? ToUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;

    /// <summary>Gom nhóm theo trạng thái ngay trên DB (GROUP BY status) rồi dựng CountByStatus.</summary>
    private static async Task<CountByStatus> ToCountByStatusAsync<TStatus>(IQueryable<TStatus> statusQuery)
        where TStatus : struct, Enum
    {
        var grouped = await statusQuery
            .GroupBy(s => s)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = new CountByStatus();
        foreach (var g in grouped)
        {
            result.ByStatus[g.Status.ToString()] = g.Count;
            result.Total += g.Count;
        }
        return result;
    }
}
