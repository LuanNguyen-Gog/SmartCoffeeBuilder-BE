using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Task thi công (construction_task) — cấp 2 của thi công 2 cấp (milestone → task), kèm ảnh hiện trường.
///
/// Role gate dưới đây là lớp phòng thủ THỨ HAI; ownership thật sự do service kiểm theo engagement
/// của milestone cha.
/// </summary>
[ApiController]
[Route("api/construction-tasks")]
[Authorize]
public class ConstructionTaskController : ControllerBase
{
    private readonly IConstructionTaskService _constructionTaskService;

    public ConstructionTaskController(IConstructionTaskService constructionTaskService)
    {
        _constructionTaskService = constructionTaskService;
    }

    /// <summary>
    /// Danh sách task; lọc theo engagement, milestone, trạng thái.
    /// Client hiển thị task của MỘT dự án phải truyền <c>projectWorkingId</c> — bỏ trống thì kết quả
    /// trải trên mọi engagement mà tài khoản tham gia, và tổng số task sẽ dính task của dự án khác.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? constructionItemId = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? projectWorkingId = null)
    {
        var result = await _constructionTaskService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, constructionItemId, status, projectWorkingId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _constructionTaskService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Constructor tạo task trong milestone.
    /// estimateAt (hạn hoàn thành) không được đặt về trước ngày hiện tại — hôm nay vẫn hợp lệ.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateConstructionTaskRequest request)
    {
        var result = await _constructionTaskService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Sửa task. Nếu có gửi estimateAt thì hạn mới không được nằm trước ngày hiện tại
    /// (hạn cũ đã trôi vào quá khứ vẫn sửa các trường khác bình thường).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConstructionTaskRequest request)
    {
        var result = await _constructionTaskService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Chuyển trạng thái: pending → in_progress → completed.</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateConstructionTaskStatusRequest request)
    {
        var result = await _constructionTaskService.UpdateStatusAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _constructionTaskService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
