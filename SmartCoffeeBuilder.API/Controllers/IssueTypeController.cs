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

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _issueTypeService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIssueTypeRequest request)
    {
        var result = await _issueTypeService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateIssueTypeRequest request)
    {
        var result = await _issueTypeService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _issueTypeService.DeleteAsync(id);
        return NoContent();
    }
}
