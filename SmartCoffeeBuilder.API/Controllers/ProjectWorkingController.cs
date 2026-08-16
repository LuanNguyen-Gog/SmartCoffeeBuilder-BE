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
        [FromQuery] Guid? projectShopOwnerId = null,
        [FromQuery] Guid? serviceProviderProfileId = null,
        [FromQuery] string? status = null)
    {
        var result = await _projectWorkingService.GetAllAsync(pageNumber, pageSize, projectShopOwnerId, serviceProviderProfileId, status);
        return Ok(result);
    }

    /// <summary>
    /// [FILTER] Lọc engagement theo NHIỀU trạng thái cùng lúc — FE dùng cho tab/bộ lọc.
    /// statuses: csv, vd "requested,accepted" (bỏ trống = lấy tất cả).
    /// contractType: design | construction | both.
    /// </summary>
    [HttpGet("filter")]
    public async Task<IActionResult> Filter(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? statuses = null,
        [FromQuery] Guid? projectShopOwnerId = null,
        [FromQuery] Guid? serviceProviderProfileId = null,
        [FromQuery] string? contractType = null)
    {
        var result = await _projectWorkingService.FilterAsync(
            pageNumber, pageSize, statuses, projectShopOwnerId, serviceProviderProfileId, contractType);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _projectWorkingService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Provider xem brief owner tạo để quyết định nhận việc — mở cho cả designer lẫn constructor,
    /// từ lúc được mời (requested); engagement rejected/terminated bị chặn.
    /// </summary>
    [HttpGet("{id:guid}/brief")]
    public async Task<IActionResult> GetBrief(Guid id)
    {
        var result = await _projectWorkingService.GetBriefAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Tổng quan dự án sau bước AI: engagement có design nhận brief + AI plan (state=completed);
    /// engagement chỉ construction nhận danh sách bản vẽ 'approved' của bên design.
    /// </summary>
    [HttpGet("{id:guid}/overview")]
    public async Task<IActionResult> GetOverview(Guid id)
    {
        var result = await _projectWorkingService.GetOverviewAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [REQUEST] Owner gửi lời mời thuê trực tiếp (không qua đăng bài).
    /// contractType: design | construction | both — phải khớp capability của provider.
    /// Engagement tạo với status=requested, chờ provider accept/reject.
    /// </summary>
    // KHÔNG mở cho admin: mời thầu là quyết định của chủ quán, không phải việc quản trị.
    [HttpPost("direct-request")]
    [Authorize(Roles = "owner")]
    public async Task<IActionResult> DirectRequest([FromBody] CreateProjectWorkingRequest request)
    {
        var result = await _projectWorkingService.CreateDirectRequestAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>[ACCEPT] Provider chấp nhận lời mời thuê trực tiếp (requested → accepted).</summary>
    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var result = await _projectWorkingService.AcceptAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>[REJECT] Provider từ chối lời mời thuê trực tiếp (requested → rejected).</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Reject(Guid id)
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
    [HttpPost("{id:guid}/request-completion")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RequestCompletion(
        Guid id, [FromBody] RequestEngagementCompletionRequest request)
    {
        var result = await _projectWorkingService.RequestCompletionAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// [OWNER — NGHIỆM THU] Owner xác nhận hoàn thành hợp tác với provider (accepted → completed).
    /// Cần contract 'confirmed'. Sau bước này review mới mở khoá.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var result = await _projectWorkingService.CompleteAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [HUỶ NGANG — BƯỚC 1] Owner hoặc provider ĐỀ NGHỊ dừng hợp tác đang chạy.
    /// KHÔNG huỷ ngay: engagement vẫn 'accepted', chỉ đặt mốc terminationRequestedAt và gửi
    /// noti + email cho bên còn lại. Chỉ khi bên kia đồng ý thì mới chuyển 'terminated'.
    /// </summary>
    [HttpPost("{id:guid}/termination-request")]
    public async Task<IActionResult> RequestTermination(
        Guid id, [FromBody] RequestEngagementTerminationRequest request)
    {
        var result = await _projectWorkingService.RequestTerminationAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// [HUỶ NGANG — BƯỚC 2] Bên CÒN LẠI phản hồi đề nghị huỷ ngang.
    /// approve=true → accepted → terminated; approve=false → xoá đề nghị, hợp tác chạy tiếp.
    /// Bên đề nghị nhận noti + email kết quả.
    /// </summary>
    [HttpPost("{id:guid}/termination-response")]
    public async Task<IActionResult> RespondTermination(
        Guid id, [FromBody] RespondEngagementTerminationRequest request)
    {
        var result = await _projectWorkingService.RespondTerminationAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// [HUỶ NGANG — RÚT LẠI] Bên đề nghị tự rút đề nghị của mình khi bên kia chưa phản hồi.
    /// Bên còn lại nhận noti + email báo không cần phản hồi nữa.
    /// </summary>
    [HttpDelete("{id:guid}/termination-request")]
    public async Task<IActionResult> CancelTerminationRequest(Guid id)
    {
        var result = await _projectWorkingService.CancelTerminationRequestAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [HUỶ NGANG — MỘT CHẠM] Cửa vào gộp, giữ tương thích ngược cho FE cũ.
    /// KHÔNG còn huỷ thẳng: chưa có đề nghị nào → tạo đề nghị (engagement vẫn 'accepted');
    /// bên kia đã đề nghị → coi như đồng ý và chuyển 'terminated'. Đọc cờ isAwaitingTerminationApproval
    /// trong response để biết đang ở bước nào. Admin gọi thì huỷ ngay (can thiệp hành chính).
    /// </summary>
    [HttpPost("{id:guid}/terminate")]
    public async Task<IActionResult> Terminate(Guid id)
    {
        var result = await _projectWorkingService.TerminateAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [QUAN HỆ] Endpoint tổng, giữ cho tương thích ngược — nên dùng các shortcut ở trên.
    /// accepted → completed (owner nghiệm thu, cần contract confirmed).
    /// status="terminated" được chuyển hướng sang luồng đồng thuận hai bên (như POST /terminate).
    /// Tiến độ design/construction là derived từ design/construction_item — không set ở đây.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateProjectWorkingStatusRequest request)
    {
        var result = await _projectWorkingService.UpdateStatusAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }
}
