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
        [FromQuery] long? projectWorkingId = null,
        [FromQuery] long? parentId = null,
        [FromQuery] string? status = null)
    {
        var result = await _constructionItemService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, projectWorkingId, parentId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _constructionItemService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Constructor tạo milestone thi công.</summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateConstructionItemRequest request)
    {
        var result = await _constructionItemService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateConstructionItemRequest request)
    {
        var result = await _constructionItemService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Chuyển trạng thái: pending → in_progress → completed.</summary>
    [HttpPut("{id:long}/status")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateConstructionItemStatusRequest request)
    {
        var result = await _constructionItemService.UpdateStatusAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(long id)
    {
        await _constructionItemService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
