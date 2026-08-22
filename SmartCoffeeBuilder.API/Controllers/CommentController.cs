using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.Comment;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Thread comment public neo vào ConstructionItem hoặc Design. FK mềm (target_type + target_id).
/// - GET: cả owner và provider liên quan tới ProjectWorking đều đọc được.
/// - POST: chỉ owner/provider liên quan hoặc admin (kiểm tra trong service).
/// - DELETE: chỉ người tạo hoặc admin.
/// </summary>
[ApiController]
[Route("api/comments")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    /// <summary>
    /// Danh sách comment theo target. targetType: construction_item | design (chấp nhận cả PascalCase).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string targetType,
        [FromQuery, BindRequired] Guid targetId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var normalized = (targetType ?? "").Trim().ToLowerInvariant().Replace("-", "_");
        if (!Enum.TryParse<CommentTargetType>(normalized, ignoreCase: true, out var parsed))
            return BadRequest(new
            {
                message = $"targetType '{targetType}' không hợp lệ. Cho phép: construction_item, design."
            });

        var result = await _commentService.GetAllAsync(parsed, targetId, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>Tạo comment. CreatedBy lấy từ token (User.GetAccountId()).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request)
    {
        var accountId = User.GetAccountId();
        var result = await _commentService.CreateAsync(request, accountId);
        return CreatedAtAction(nameof(GetAll), new
        {
            targetType = result.TargetType,
            targetId = result.TargetId
        }, result);
    }

    /// <summary>Xoá comment — service kiểm tra người tạo hoặc admin.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var accountId = User.GetAccountId();
        await _commentService.DeleteAsync(id, accountId);
        return NoContent();
    }
}