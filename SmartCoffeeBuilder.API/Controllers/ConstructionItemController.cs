using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Milestone thi công (construction_item) — cấp 1 của thi công 2 cấp (milestone → task).
/// Chỉ tạo được khi engagement 'accepted' + có contract 'confirmed' và contract_type có construction.
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
        [FromQuery] long? projectWorkingId = null,
        [FromQuery] long? parentId = null,
        [FromQuery] string? status = null)
    {
        var result = await _constructionItemService.GetAllAsync(pageNumber, pageSize, projectWorkingId, parentId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _constructionItemService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Constructor tạo milestone thi công.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConstructionItemRequest request)
    {
        var result = await _constructionItemService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateConstructionItemRequest request)
    {
        var result = await _constructionItemService.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Chuyển trạng thái: pending → in_progress → completed.</summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateConstructionItemStatusRequest request)
    {
        var result = await _constructionItemService.UpdateStatusAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _constructionItemService.DeleteAsync(id);
        return NoContent();
    }
}
