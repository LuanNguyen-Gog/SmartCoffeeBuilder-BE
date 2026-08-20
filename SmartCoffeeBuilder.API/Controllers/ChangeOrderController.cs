using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ChangeOrder;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Phát sinh chi phí ngoài báo giá đã chốt (review 1.1: "quy định số lần sửa và phí sửa").
///
/// Không có role gate: cả hai bên đều lập được khoản phát sinh, nhưng bên KIA mới duyệt. Luật đó
/// nằm ở <c>ChangeOrderService.RespondAsync</c> — role gate không phân biệt nổi "bên nào của hợp
/// tác nào" (xem mục Authorization trong CLAUDE.md).
/// </summary>
[ApiController]
[Route("api/change-orders")]
[Authorize]
public class ChangeOrderController : ControllerBase
{
    private readonly IChangeOrderService _changeOrderService;

    public ChangeOrderController(IChangeOrderService changeOrderService)
    {
        _changeOrderService = changeOrderService;
    }

    /// <summary>Các khoản phát sinh của một hợp tác. Lọc thêm bằng <c>status</c>.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid projectWorkingId,
        [FromQuery] string? status = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _changeOrderService.GetAllAsync(
            User.GetAccountId(), projectWorkingId, status, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>Tổng công nợ: giá trị hợp đồng + các khoản phát sinh đã duyệt.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid projectWorkingId)
    {
        var result = await _changeOrderService.GetSummaryAsync(User.GetAccountId(), projectWorkingId);
        return Ok(result);
    }

    /// <summary>
    /// Hạn mức sửa của một bản thiết kế: đã dùng mấy vòng, còn mấy vòng miễn phí, vòng kế tiếp
    /// tốn bao nhiêu. FE gọi trước khi cho owner bấm "yêu cầu chỉnh sửa".
    /// </summary>
    [HttpGet("revision-quota/{designId:guid}")]
    public async Task<IActionResult> GetRevisionQuota(Guid designId)
    {
        var result = await _changeOrderService.GetRevisionQuotaAsync(User.GetAccountId(), designId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _changeOrderService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChangeOrderRequest request)
    {
        var result = await _changeOrderService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Sửa khoản còn 'pending' — chỉ bên đã lập.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateChangeOrderRequest request)
    {
        var result = await _changeOrderService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Bên KIA đồng ý — khoản được khoá và cộng vào công nợ.</summary>
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var result = await _changeOrderService.AcceptAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Bên KIA từ chối, kèm lý do.</summary>
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectChangeOrderRequest request)
    {
        var result = await _changeOrderService.RejectAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Bên đã lập rút lại khoản còn 'pending'. Khoản đã duyệt/từ chối là vết, không xoá.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _changeOrderService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
