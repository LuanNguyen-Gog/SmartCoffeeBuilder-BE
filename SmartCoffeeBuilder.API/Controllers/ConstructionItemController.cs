using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Milestone thi công (construction_item) — cấp 1 của thi công 2 cấp (milestone → task).
/// Chỉ tạo được khi engagement 'accepted' + có contract 'confirmed' và contract_type có construction.
///
/// Role gate dưới đây là lớp phòng thủ THỨ HAI; ownership thật sự do service kiểm theo engagement.
/// </summary>
[ApiController]
[Route("api/construction-items")]
[Authorize]
public class ConstructionItemController : ControllerBase
{
    private readonly IConstructionItemService _constructionItemService;

    public ConstructionItemController(IConstructionItemService constructionItemService)
    {
        _constructionItemService = constructionItemService;
    }

    /// <summary>Danh sách milestone; lọc theo engagement, milestone cha, trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] Guid? parentId = null,
        [FromQuery] string? status = null)
    {
        var result = await _constructionItemService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, projectWorkingId, parentId, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _constructionItemService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Constructor tạo milestone thi công.
    /// estimateAt (hạn hoàn thành) không được đặt về trước ngày hiện tại — hôm nay vẫn hợp lệ.
    /// parentId chỉ được trỏ tới milestone GỐC (parentId = null): cây thi công đúng 2 cấp,
    /// muốn chia nhỏ thêm thì tạo task bên trong milestone con.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateConstructionItemRequest request)
    {
        var result = await _constructionItemService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Sửa milestone. Nếu có gửi estimateAt thì hạn mới không được nằm trước ngày hiện tại
    /// (hạn cũ đã trôi vào quá khứ vẫn sửa các trường khác bình thường).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConstructionItemRequest request)
    {
        var result = await _constructionItemService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Chuyển trạng thái: pending → in_progress → completed.
    /// Sang 'completed' chỉ được khi MỌI task con VÀ MỌI milestone con đã 'completed' — còn
    /// việc dở thì trả 409 kèm số task / milestone con chưa xong.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateConstructionItemStatusRequest request)
    {
        var result = await _constructionItemService.UpdateStatusAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _constructionItemService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }

    /// <summary>
    /// Chi phí của một hạng mục: nhân công + vật tư, gộp task con và milestone con
    /// (review 1.1: "quản lý thi công theo … chi phí").
    /// </summary>
    [HttpGet("{id:guid}/cost-summary")]
    public async Task<IActionResult> GetCostSummary(Guid id)
    {
        var result = await _constructionItemService.GetCostSummaryAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Chi phí thi công của cả hợp tác — dự toán, thực chi và chênh lệch.</summary>
    [HttpGet("cost-summary")]
    public async Task<IActionResult> GetEngagementCostSummary([FromQuery] Guid projectWorkingId)
    {
        var result = await _constructionItemService.GetEngagementCostSummaryAsync(
            User.GetAccountId(), projectWorkingId);
        return Ok(result);
    }
}
