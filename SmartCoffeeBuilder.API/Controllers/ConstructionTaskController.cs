using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Task thi công (construction_task) — cấp 2 của thi công 2 cấp (milestone → task), kèm ảnh hiện trường.
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

    /// <summary>Danh sách task; lọc theo milestone, trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? constructionItemId = null,
        [FromQuery] string? status = null)
    {
        var result = await _constructionTaskService.GetAllAsync(pageNumber, pageSize, constructionItemId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _constructionTaskService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Constructor tạo task trong milestone.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConstructionTaskRequest request)
    {
        var result = await _constructionTaskService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateConstructionTaskRequest request)
    {
        var result = await _constructionTaskService.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Chuyển trạng thái: pending → in_progress → completed.</summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateConstructionTaskStatusRequest request)
    {
        var result = await _constructionTaskService.UpdateStatusAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _constructionTaskService.DeleteAsync(id);
        return NoContent();
    }
}
