using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.IssueType;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>Danh mục loại issue (lookup) — admin quản lý.</summary>
[ApiController]
[Route("api/issue-types")]
[Authorize]
public class IssueTypeController : ControllerBase
{
    private readonly IIssueTypeService _issueTypeService;

    public IssueTypeController(IIssueTypeService issueTypeService)
    {
        _issueTypeService = issueTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _issueTypeService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _issueTypeService.GetByIdAsync(id);
        return Ok(result);
    }

    // Danh mục dùng chung toàn hệ thống — chỉ admin sửa, mọi người đọc.
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreateIssueTypeRequest request)
    {
        var result = await _issueTypeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateIssueTypeRequest request)
    {
        var result = await _issueTypeService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _issueTypeService.DeleteAsync(id);
        return NoContent();
    }
}
