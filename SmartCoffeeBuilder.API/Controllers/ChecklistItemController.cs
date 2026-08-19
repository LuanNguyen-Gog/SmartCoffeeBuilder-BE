using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Checklist;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Checklist nghiệm thu (review 3): mỗi bản thiết kế / hạng mục thi công có danh sách mục cần
/// nghiệm thu. Provider lập mục và đính minh chứng; CHỦ QUÁN là người chấm đạt / chưa đạt kèm ghi
/// chú "cần sửa gì".
/// </summary>
[ApiController]
[Route("api/checklist-items")]
[Authorize]
public class ChecklistItemController : ControllerBase
{
    private readonly IChecklistItemService _checklistItemService;

    public ChecklistItemController(IChecklistItemService checklistItemService)
    {
        _checklistItemService = checklistItemService;
    }

    /// <summary>Checklist của một bản thiết kế (<c>designId</c>) hoặc hạng mục thi công (<c>constructionItemId</c>).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] Guid? designId = null,
        [FromQuery] Guid? constructionItemId = null,
        [FromQuery] string? status = null)
    {
        var result = await _checklistItemService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, designId, constructionItemId, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _checklistItemService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Nhà cung cấp lập các mục nghiệm thu (nhập nhiều mục một lần).</summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateChecklistItemsRequest request)
    {
        var result = await _checklistItemService.CreateAsync(User.GetAccountId(), request);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateChecklistItemRequest request)
    {
        var result = await _checklistItemService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Chủ quán chấm một mục: <c>passed</c> hoặc <c>failed</c>. Chấm 'failed' bắt buộc kèm ghi chú
    /// chưa đạt ở chỗ nào — chấm lại được sau khi nhà cung cấp sửa.
    /// </summary>
    [HttpPost("{id:guid}/check")]
    [Authorize(Roles = "owner")]
    public async Task<IActionResult> Check(Guid id, [FromBody] CheckChecklistItemRequest request)
    {
        var result = await _checklistItemService.CheckAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Nhà cung cấp đính minh chứng cho một mục (ảnh hiện trường, biên bản…).</summary>
    [HttpPost("{id:guid}/evidence")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> AttachEvidence(
        Guid id, [FromBody] AttachChecklistEvidenceRequest request)
    {
        var result = await _checklistItemService.AttachEvidenceAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Xoá mục chưa chấm. Mục đã chấm được giữ lại làm vết nghiệm thu.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _checklistItemService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
