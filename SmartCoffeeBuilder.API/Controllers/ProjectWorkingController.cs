using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

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
        var result = await _projectWorkingService.AcceptAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>[REJECT] Provider từ chối lời mời thuê trực tiếp (requested → rejected).</summary>
    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id)
    {
        var result = await _projectWorkingService.RejectAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [PROVIDER — BÁO XONG VIỆC] Designer/constructor báo đã hoàn thành phần việc, mời owner nghiệm thu.
    /// KHÔNG đổi provider_status (vẫn 'accepted') — chỉ đặt mốc completionRequestedAt để FE hiện
    /// nút "Nghiệm thu" cho owner (cờ isAwaitingAcceptance).
    /// Điều kiện: engagement 'accepted', có contract 'confirmed', và deliverable đã xong
    /// (design: ít nhất 1 bản 'approved'; construction: mọi milestone 'completed').
    /// </summary>
    [HttpPost("{id:long}/request-completion")]
    public async Task<IActionResult> RequestCompletion(
        long id, [FromBody] RequestEngagementCompletionRequest request)
    {
        var result = await _projectWorkingService.RequestCompletionAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// [OWNER — NGHIỆM THU] Owner xác nhận hoàn thành hợp tác với provider (accepted → completed).
    /// Cần contract 'confirmed'. Sau bước này review mới mở khoá.
    /// </summary>
    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id)
    {
        var result = await _projectWorkingService.CompleteAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>[HUỶ NGANG] Owner hoặc provider dừng hợp tác đang chạy (accepted → terminated).</summary>
    [HttpPost("{id:long}/terminate")]
    public async Task<IActionResult> Terminate(long id)
    {
        var result = await _projectWorkingService.TerminateAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [QUAN HỆ] Endpoint tổng, giữ cho tương thích ngược — nên dùng các shortcut ở trên.
    /// accepted → completed (owner nghiệm thu, cần contract confirmed) | terminated (huỷ ngang).
    /// Tiến độ design/construction là derived từ design/construction_item — không set ở đây.
    /// </summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateProjectWorkingStatusRequest request)
    {
        var result = await _projectWorkingService.UpdateStatusAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }
}
