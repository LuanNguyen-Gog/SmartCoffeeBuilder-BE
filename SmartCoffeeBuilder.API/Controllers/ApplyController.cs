using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Apply;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/applies")]
[Authorize]
public class ApplyController : ControllerBase
{
    private readonly IApplyService _applyService;

    public ApplyController(IApplyService applyService)
    {
        _applyService = applyService;
    }

    /// <summary>Danh sách hồ sơ ứng tuyển. status: pending | accepted | rejected.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? postId = null,
        [FromQuery] Guid? serviceProviderProfileId = null,
        [FromQuery] string? status = null)
    {
        var result = await _applyService.GetAllAsync(pageNumber, pageSize, postId, serviceProviderProfileId, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _applyService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// [ỨNG TUYỂN] Provider nộp hồ sơ vào bài đang open, còn hạn.
    /// Hồ sơ provider lấy từ tài khoản đang đăng nhập (JWT) — không cần gửi serviceProviderProfileId.
    /// Capability của provider phải khớp serviceKind của bài (designer↔design, constructor↔construction, both↔mọi loại).
    /// </summary>
    // KHÔNG mở cho admin: hồ sơ provider resolve từ token, tài khoản admin không có
    // ServiceProviderProfile nên vào cũng chỉ nhận 404 khó hiểu.
    [HttpPost("apply")]
    [Authorize(Roles = "provider")]
    public async Task<IActionResult> Apply([FromBody] CreateApplyRequest request)
    {
        var result = await _applyService.ApplyAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Provider sửa proposal / thời gian dự kiến — chỉ khi hồ sơ còn pending.</summary>
    [HttpPut("{id:guid}/proposal")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateProposal(Guid id, [FromBody] UpdateApplyRequest request)
    {
        var result = await _applyService.UpdateProposalAsync(id, request);
        return Ok(result);
    }

    /// <summary>Owner chấp nhận hồ sơ — trả về engagement (project_provider) vừa tạo.</summary>
    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var result = await _applyService.AcceptAsync(id);
        return Ok(result);
    }

    /// <summary>Owner từ chối hồ sơ.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Reject(Guid id)
    {
        var result = await _applyService.RejectAsync(id);
        return Ok(result);
    }

    /// <summary>Provider rút hồ sơ khi còn pending — xoá hẳn bản ghi.</summary>
    [HttpDelete("{id:guid}/withdraw")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        await _applyService.WithdrawAsync(id);
        return NoContent();
    }
}
