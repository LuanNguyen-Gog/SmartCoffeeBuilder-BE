using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Engagement giữa project và provider.
/// Lưu ý: API "tìm người" để thuê trực tiếp là GET /api/service-provider-profiles (lọc capability/isVerified/search).
/// </summary>
[ApiController]
[Route("api/project-workings")]
[Authorize]
public class ProjectWorkingController : ControllerBase
{
    private readonly IProjectWorkingService _projectWorkingService;

    public ProjectWorkingController(IProjectWorkingService projectWorkingService)
    {
        _projectWorkingService = projectWorkingService;
    }

    /// <summary>
    /// Danh sách engagement. status: requested | accepted | rejected | completed | terminated.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectShopOwnerId = null,
        [FromQuery] long? serviceProviderProfileId = null,
        [FromQuery] string? status = null)
    {
        var result = await _projectWorkingService.GetAllAsync(pageNumber, pageSize, projectShopOwnerId, serviceProviderProfileId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _projectWorkingService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Provider xem brief owner tạo để quyết định nhận việc — mở cho cả designer lẫn constructor,
    /// từ lúc được mời (requested); engagement rejected/terminated bị chặn.
    /// </summary>
    [HttpGet("{id:long}/brief")]
    public async Task<IActionResult> GetBrief(long id)
    {
        var result = await _projectWorkingService.GetBriefAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Tổng quan dự án sau bước AI: engagement có design nhận brief + AI plan (state=completed);
    /// engagement chỉ construction nhận danh sách bản vẽ 'approved' của bên design.
    /// </summary>
    [HttpGet("{id:long}/overview")]
    public async Task<IActionResult> GetOverview(long id)
    {
        var result = await _projectWorkingService.GetOverviewAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// [REQUEST] Owner gửi lời mời thuê trực tiếp (không qua đăng bài).
    /// contractType: design | construction | both — phải khớp capability của provider.
    /// Engagement tạo với status=requested, chờ provider accept/reject.
    /// </summary>
    [HttpPost("direct-request")]
    public async Task<IActionResult> DirectRequest([FromBody] CreateProjectWorkingRequest request)
    {
        var result = await _projectWorkingService.CreateDirectRequestAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>[ACCEPT] Provider chấp nhận lời mời thuê trực tiếp (requested → accepted).</summary>
    [HttpPost("{id:long}/accept")]
    public async Task<IActionResult> Accept(long id)
    {
        var result = await _projectWorkingService.UpdateStatusAsync(
            id, new UpdateProjectWorkingStatusRequest { Status = "accepted" });
        return Ok(result);
    }

    /// <summary>[REJECT] Provider từ chối lời mời thuê trực tiếp (requested → rejected).</summary>
    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id)
    {
        var result = await _projectWorkingService.UpdateStatusAsync(
            id, new UpdateProjectWorkingStatusRequest { Status = "rejected" });
        return Ok(result);
    }

    /// <summary>
    /// [QUAN HỆ] Chuyển trạng thái engagement (v5 — không phải tiến độ):
    /// accepted → completed (owner nghiệm thu, cần contract confirmed) | terminated (huỷ ngang).
    /// Tiến độ design/construction là derived từ design/construction_item — không set ở đây.
    /// </summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateProjectWorkingStatusRequest request)
    {
        var result = await _projectWorkingService.UpdateStatusAsync(id, request);
        return Ok(result);
    }
}
