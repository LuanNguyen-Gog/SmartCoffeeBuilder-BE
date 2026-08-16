using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Issue;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Issue (sự cố/phát sinh) — dùng chung design-phase và construction-phase, neo vào engagement.
/// Có thể gắn vào một milestone thi công (construction_item).
/// </summary>
[ApiController]
[Route("api/issues")]
[Authorize]
public class IssueController : ControllerBase
{
    private readonly IIssueService _issueService;

    public IssueController(IIssueService issueService)
    {
        _issueService = issueService;
    }

    /// <summary>Danh sách issue; lọc theo engagement, milestone, trạng thái.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] Guid? constructionItemId = null,
        [FromQuery] string? status = null)
    {
        var result = await _issueService.GetAllAsync(pageNumber, pageSize, projectWorkingId, constructionItemId, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _issueService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueRequest request)
    {
        var result = await _issueService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIssueRequest request)
    {
        var result = await _issueService.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Chuyển trạng thái: open → in_progress → resolved → closed.</summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateIssueStatusRequest request)
    {
        var result = await _issueService.UpdateStatusAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _issueService.DeleteAsync(id);
        return NoContent();
    }
}
