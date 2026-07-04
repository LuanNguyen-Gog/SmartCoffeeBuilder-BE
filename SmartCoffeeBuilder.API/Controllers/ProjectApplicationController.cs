using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectApplication;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/project-applications")]
[Authorize]
public class ProjectApplicationController : ControllerBase
{
    private readonly IProjectApplicationService _projectApplicationService;

    public ProjectApplicationController(IProjectApplicationService projectApplicationService)
    {
        _projectApplicationService = projectApplicationService;
    }

    /// <summary>Danh sách hồ sơ ứng tuyển. status: pending | accepted | rejected.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? postId = null,
        [FromQuery] long? providerId = null,
        [FromQuery] string? status = null)
    {
        var result = await _projectApplicationService.GetAllAsync(pageNumber, pageSize, postId, providerId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _projectApplicationService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// [ỨNG TUYỂN] Provider nộp hồ sơ vào bài đang open, còn hạn.
    /// Capability của provider phải khớp serviceKind của bài (designer↔design, constructor↔construction, both↔mọi loại).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectApplicationRequest request)
    {
        var result = await _projectApplicationService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectApplicationRequest request)
    {
        var result = await _projectApplicationService.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Owner chấp nhận hồ sơ — trả về engagement (project_provider) vừa tạo.</summary>
    [HttpPost("{id:long}/accept")]
    public async Task<IActionResult> Accept(long id)
    {
        var result = await _projectApplicationService.AcceptAsync(id);
        return Ok(result);
    }

    /// <summary>Owner từ chối hồ sơ.</summary>
    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id)
    {
        var result = await _projectApplicationService.RejectAsync(id);
        return Ok(result);
    }

    /// <summary>Provider rút hồ sơ khi còn pending.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _projectApplicationService.DeleteAsync(id);
        return NoContent();
    }
}
