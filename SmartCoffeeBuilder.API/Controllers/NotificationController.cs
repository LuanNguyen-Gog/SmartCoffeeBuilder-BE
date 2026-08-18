using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Thông báo in-app + email. Lịch sử noti cho FE/mobile, đánh dấu đã đọc, và gửi lại email.
/// Noti được tạo tự động bởi các luồng nghiệp vụ (vd ứng tuyển / chấp nhận / từ chối hồ sơ).
///
/// Mọi endpoint chỉ thao tác trên noti CỦA CHÍNH người đang đăng nhập — accountId lấy từ JWT,
/// KHÔNG nhận từ query. Trước đây nhận từ query nên bất kỳ ai cũng đọc được hộp thư người khác.
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

    /// <summary>Lịch sử noti của chính mình (mới nhất trước). isRead=false để lấy chưa đọc.</summary>
    [HttpGet]
    public async Task<IActionResult> GetForAccount(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isRead = null)
    {
        var result = await _notificationService.GetForAccountAsync(
            User.GetAccountId(), pageNumber, pageSize, isRead);
        return Ok(result);
    }

    /// <summary>Số noti chưa đọc của chính mình — cho badge.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var accountId = User.GetAccountId();
        var count = await _notificationService.GetUnreadCountAsync(accountId);
        return Ok(new { accountId, unreadCount = count });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _notificationService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Đánh dấu một noti đã đọc.</summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id)
    {
        var result = await _notificationService.MarkAsReadAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Đánh dấu tất cả noti của chính mình đã đọc.</summary>
    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var accountId = User.GetAccountId();
        var updated = await _notificationService.MarkAllAsReadAsync(accountId);
        return Ok(new { accountId, updated });
    }

    /// <summary>Gửi lại email cho một noti của chính mình (vd lần trước gửi lỗi).</summary>
    [HttpPost("{id:guid}/resend")]
    public async Task<IActionResult> Resend(Guid id)
    {
        var result = await _notificationService.ResendAsync(User.GetAccountId(), id);
        return Ok(result);
    }
}
