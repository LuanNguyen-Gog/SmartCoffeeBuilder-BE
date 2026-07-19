using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Thông báo in-app + email. Lịch sử noti cho FE/mobile, đánh dấu đã đọc, và gửi lại email.
/// Noti được tạo tự động bởi các luồng nghiệp vụ (vd ứng tuyển / chấp nhận / từ chối hồ sơ).
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Lịch sử noti của một account (mới nhất trước). isRead=false để lấy chưa đọc.</summary>
    [HttpGet]
    public async Task<IActionResult> GetForAccount(
        [FromQuery] long accountId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null)
    {
        var result = await _notificationService.GetForAccountAsync(accountId, pageNumber, pageSize, isRead);
        return Ok(result);
    }

    /// <summary>Số noti chưa đọc của account — cho badge.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount([FromQuery] long accountId)
    {
        var count = await _notificationService.GetUnreadCountAsync(accountId);
        return Ok(new { accountId, unreadCount = count });
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _notificationService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Đánh dấu một noti đã đọc.</summary>
    [HttpPatch("{id:long}/read")]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var result = await _notificationService.MarkAsReadAsync(id);
        return Ok(result);
    }

    /// <summary>Đánh dấu tất cả noti của account đã đọc.</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead([FromQuery] long accountId)
    {
        var updated = await _notificationService.MarkAllAsReadAsync(accountId);
        return Ok(new { accountId, updated });
    }

    /// <summary>Gửi lại email cho một noti (vd lần trước gửi lỗi).</summary>
    [HttpPost("{id:long}/resend")]
    public async Task<IActionResult> Resend(long id)
    {
        var result = await _notificationService.ResendAsync(id);
        return Ok(result);
    }
}
