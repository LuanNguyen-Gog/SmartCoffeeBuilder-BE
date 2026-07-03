using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectProvider;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Engagement giữa project và provider.
/// Lưu ý: API "tìm người" để thuê trực tiếp là GET /api/service-providers (lọc capability/isVerified/search).
/// </summary>
[ApiController]
[Route("api/project-providers")]
[Authorize]
public class ProjectProviderController : ControllerBase
{
    private readonly IProjectProviderService _projectProviderService;

    public ProjectProviderController(IProjectProviderService projectProviderService)
    {
        _projectProviderService = projectProviderService;
    }

    /// <summary>
    /// Danh sách engagement. status: requested | accepted | rejected | designing | designed |
    /// constructing | constructed | completed | terminated.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectId = null,
        [FromQuery] long? providerId = null,
        [FromQuery] string? status = null)
    {
        var result = await _projectProviderService.GetAllAsync(pageNumber, pageSize, projectId, providerId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _projectProviderService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// [REQUEST] Owner gửi lời mời thuê trực tiếp (không qua đăng bài).
    /// contractType: design | construction | both — phải khớp capability của provider.
    /// Engagement tạo với status=requested, chờ provider accept/reject.
    /// </summary>
    [HttpPost("direct-request")]
    public async Task<IActionResult> DirectRequest([FromBody] CreateProjectProviderRequest request)
    {
        var result = await _projectProviderService.CreateDirectRequestAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>[ACCEPT] Provider chấp nhận lời mời thuê trực tiếp (requested → accepted).</summary>
    [HttpPost("{id:long}/accept")]
    public async Task<IActionResult> Accept(long id)
    {
        var result = await _projectProviderService.UpdateStatusAsync(
            id, new UpdateProjectProviderStatusRequest { Status = "accepted" });
        return Ok(result);
    }

    /// <summary>[REJECT] Provider từ chối lời mời thuê trực tiếp (requested → rejected).</summary>
    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id)
    {
        var result = await _projectProviderService.UpdateStatusAsync(
            id, new UpdateProjectProviderStatusRequest { Status = "rejected" });
        return Ok(result);
    }

    /// <summary>
    /// [TIẾN ĐỘ] Chuyển trạng thái công việc sau khi đã accepted:
    /// accepted → designing (design/both) | constructing (construction);
    /// designing → designed; designed → constructing (both) | completed;
    /// constructing → constructed; constructed → completed;
    /// terminated: kết thúc sớm khi đang hoạt động.
    /// </summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateProjectProviderStatusRequest request)
    {
        var result = await _projectProviderService.UpdateStatusAsync(id, request);
        return Ok(result);
    }
}
