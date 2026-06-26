using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/project-posts")]
[Authorize]
public class ProjectPostController : ControllerBase
{
    private readonly IProjectPostService _projectPostService;

    public ProjectPostController(IProjectPostService projectPostService)
    {
        _projectPostService = projectPostService;
    }

    /// <summary>Tìm/duyệt bài đăng (provider lọc status=open, serviceKind theo capability).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectId = null,
        [FromQuery] string? serviceKind = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _projectPostService.GetAllAsync(
            pageNumber, pageSize, projectId, serviceKind, status, search);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _projectPostService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Owner đăng bài tuyển provider cho project.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectPostRequest request)
    {
        var result = await _projectPostService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectPostRequest request)
    {
        var result = await _projectPostService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _projectPostService.DeleteAsync(id);
        return NoContent();
    }
}
