using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Admin;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// API quản trị (chỉ admin): thống kê nền tảng, báo cáo doanh thu, quản lý tài khoản.
/// Toàn bộ endpoint yêu cầu JWT có role 'admin'.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IAccountService _accountService;

    public AdminController(IAdminService adminService, IAccountService accountService)
    {
        _adminService = adminService;
        _accountService = accountService;
    }

    // ───────────────────────── Thống kê ─────────────────────────

    /// <summary>Bảng tổng quan dashboard: tài khoản, dự án, marketplace, hợp đồng, subscription, doanh thu.</summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview()
    {
        var result = await _adminService.GetOverviewAsync();
        return Ok(result);
    }

    /// <summary>Thống kê tài khoản theo vai trò/trạng thái.</summary>
    [HttpGet("statistics/accounts")]
    public async Task<IActionResult> GetAccountStatistics()
    {
        var result = await _adminService.GetAccountStatisticsAsync();
        return Ok(result);
    }

    // ───────────────────────── Doanh thu ─────────────────────────

    /// <summary>
    /// Báo cáo doanh thu (giao dịch payOS 'paid'). from/to là UTC (ISO 8601), groupBy = day | month.
    /// </summary>
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string groupBy = "month")
    {
        var result = await _adminService.GetRevenueReportAsync(from, to, groupBy);
        return Ok(result);
    }

    /// <summary>Danh sách giao dịch payOS (drill-down), lọc theo status/purpose/khoảng thời gian.</summary>
    [HttpGet("revenue/transactions")]
    public async Task<IActionResult> GetRevenueTransactions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? purpose = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _adminService.GetRevenueTransactionsAsync(pageNumber, pageSize, status, purpose, from, to);
        return Ok(result);
    }

    // ───────────────────────── Quản lý tài khoản ─────────────────────────

    /// <summary>Liệt kê tài khoản có lọc: role, status, search (email/phone), includeDeleted.</summary>
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeDeleted = false)
    {
        var result = await _accountService.SearchAsync(pageNumber, pageSize, role, status, search, includeDeleted);
        return Ok(result);
    }

    [HttpGet("accounts/{id:guid}")]
    public async Task<IActionResult> GetAccount(Guid id)
    {
        var result = await _accountService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Đổi trạng thái tài khoản (khoá/mở khoá): active | inactive | banned | pending.</summary>
    [HttpPatch("accounts/{id:guid}/status")]
    public async Task<IActionResult> SetAccountStatus(Guid id, [FromBody] SetAccountStatusRequest request)
    {
        var result = await _accountService.SetStatusAsync(id, request.Status);
        return Ok(result);
    }

    /// <summary>Xoá mềm tài khoản.</summary>
    [HttpDelete("accounts/{id:guid}")]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        await _accountService.DeleteAsync(id);
        return NoContent();
    }
}
