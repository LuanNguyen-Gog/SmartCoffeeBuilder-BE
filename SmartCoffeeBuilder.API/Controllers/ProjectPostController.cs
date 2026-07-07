using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectPost;
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

    /// <summary>
    /// [TÌM BÀI] Provider tìm bài đăng. serviceKind: design | construction | both.
    /// status: open | closed | cancelled (lọc open tự ẩn bài quá hạn nộp). search: theo title.
    /// </summary>
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

    /// <summary>
    /// [ĐĂNG BÀI] Owner đăng bài tuyển provider. serviceKind: design | construction | both.
    /// Bài tạo với status=open; submissionDeadline (nếu có) phải ở tương lai.
    /// </summary>
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
