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
        [FromQuery] long? postId = null,
        [FromQuery] long? serviceProviderProfileId = null,
        [FromQuery] string? status = null)
    {
        var result = await _applyService.GetAllAsync(pageNumber, pageSize, postId, serviceProviderProfileId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _applyService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// [ỨNG TUYỂN] Provider nộp hồ sơ vào bài đang open, còn hạn.
    /// Hồ sơ provider lấy từ tài khoản đang đăng nhập (JWT) — không cần gửi serviceProviderProfileId.
    /// Capability của provider phải khớp serviceKind của bài (designer↔design, constructor↔construction, both↔mọi loại).
    /// </summary>
    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] CreateApplyRequest request)
    {
        var result = await _applyService.ApplyAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Provider sửa proposal / thời gian dự kiến — chỉ khi hồ sơ còn pending.</summary>
    [HttpPut("{id:long}/proposal")]
    public async Task<IActionResult> UpdateProposal(long id, [FromBody] UpdateApplyRequest request)
    {
        var result = await _applyService.UpdateProposalAsync(id, request);
        return Ok(result);
    }

    /// <summary>Owner chấp nhận hồ sơ — trả về engagement (project_provider) vừa tạo.</summary>
    [HttpPost("{id:long}/accept")]
    public async Task<IActionResult> Accept(long id)
    {
        var result = await _applyService.AcceptAsync(id);
        return Ok(result);
    }

    /// <summary>Owner từ chối hồ sơ.</summary>
    [HttpPost("{id:long}/reject")]
    public async Task<IActionResult> Reject(long id)
    {
        var result = await _applyService.RejectAsync(id);
        return Ok(result);
    }

    /// <summary>Provider rút hồ sơ khi còn pending — xoá hẳn bản ghi.</summary>
    [HttpDelete("{id:long}/withdraw")]
    public async Task<IActionResult> Withdraw(long id)
    {
        await _applyService.WithdrawAsync(id);
        return NoContent();
    }
}
