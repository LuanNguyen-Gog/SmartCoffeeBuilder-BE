using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.DailyLog;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Nhật ký thi công hằng ngày (review 3): nhà cung cấp báo cáo mỗi ngày đã làm gì, kèm ảnh/video
/// hiện trường và vấn đề phát sinh; chủ quán theo dõi tiến độ mà không phải ra công trường.
/// Gắn được vào hạng mục hoặc task cụ thể để xem chi tiết quá trình làm việc đó.
/// </summary>
[ApiController]
[Route("api/daily-logs")]
[Authorize]
public class DailyLogController : ControllerBase
{
    private readonly IDailyLogService _dailyLogService;

    public DailyLogController(IDailyLogService dailyLogService)
    {
        _dailyLogService = dailyLogService;
    }

    /// <summary>
    /// Nhật ký theo engagement / hạng mục / task, mới nhất trước. Lọc thêm theo khoảng ngày
    /// (<c>fromDate</c>, <c>toDate</c>) để xem báo cáo của một tuần/tháng.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] Guid? constructionItemId = null,
        [FromQuery] Guid? constructionTaskId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null)
    {
        var result = await _dailyLogService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize,
            projectWorkingId, constructionItemId, constructionTaskId, fromDate, toDate);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _dailyLogService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Nhà cung cấp ghi nhật ký cho một ngày.</summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateDailyLogRequest request)
    {
        var result = await _dailyLogService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Sửa nhật ký. Field null = giữ nguyên; riêng <c>media</c> khác null thì thay toàn bộ danh
    /// sách file (mảng rỗng để gỡ hết).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDailyLogRequest request)
    {
        var result = await _dailyLogService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _dailyLogService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
