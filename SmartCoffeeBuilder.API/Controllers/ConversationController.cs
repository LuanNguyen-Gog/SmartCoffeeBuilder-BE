using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Chat;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Quản lý thread (Conversation) trong một engagement. Polling thay vì SignalR —
/// FE gọi <c>GET /api/chat/messages?conversationId=X&amp;sinceId=Y</c> mỗi vài giây.
/// </summary>
[ApiController]
[Route("api/chat/conversations")]
[Authorize]
public class ConversationController : ControllerBase
{
    private readonly IConversationService _service;

    public ConversationController(IConversationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Liệt kê thread trong một engagement — sort theo hoạt động (UpdatedAt DESC).
    /// Phân quyền: chỉ owner hoặc provider của engagement.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetByEngagement(
        [FromQuery] long projectWorkingId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetByEngagementAsync(
            User.GetAccountId(), projectWorkingId, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>Chi tiết một thread kèm danh sách message phân trang (SentAt ASC).</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(
        long id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _service.GetByIdAsync(
            User.GetAccountId(), id, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Tạo thread mới trong engagement. Topic rỗng/khoảng trắng → service tự đặt "Thread #N".
    /// Phân quyền: chỉ owner hoặc provider của engagement.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequest request)
    {
        var result = await _service.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Đổi tên thread — cả owner và provider đều được sửa.</summary>
    [HttpPatch("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateConversationRequest request)
    {
        var result = await _service.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Xoá thread — chỉ creator. Cascade xoá message và attachment.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _service.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}