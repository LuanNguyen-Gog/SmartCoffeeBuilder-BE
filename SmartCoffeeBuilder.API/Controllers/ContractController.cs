using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Contract;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Hợp đồng engagement (OTP gate): drafted → pending_otp → confirmed; huỷ khi chưa confirmed.
/// `confirmed` là mốc mở khoá tạo design/construction_item (không đổi provider_status).
/// </summary>
[ApiController]
[Route("api/contracts")]
[Authorize]
public class ContractController : ControllerBase
{
    private readonly IContractService _contractService;

    public ContractController(IContractService contractService)
    {
        _contractService = contractService;
    }

    /// <summary>Danh sách hợp đồng, lọc theo engagement.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectWorkingId = null)
    {
        var result = await _contractService.GetAllAsync(pageNumber, pageSize, projectWorkingId);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _contractService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Provider tạo bản hợp đồng (draft) cho engagement 'accepted'.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        var result = await _contractService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật nội dung hợp đồng — chỉ khi còn 'drafted'.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateContractRequest request)
    {
        var result = await _contractService.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Gửi OTP ký hợp đồng cho owner (drafted → pending_otp).</summary>
    [HttpPost("{id:long}/send-otp")]
    public async Task<IActionResult> SendOtp(long id)
    {
        var result = await _contractService.SendOtpAsync(id);
        return Ok(result);
    }

    /// <summary>Owner xác nhận OTP ký hợp đồng (pending_otp → confirmed).</summary>
    [HttpPost("{id:long}/confirm-otp")]
    public async Task<IActionResult> ConfirmOtp(long id, [FromBody] ConfirmContractOtpRequest request)
    {
        var result = await _contractService.ConfirmOtpAsync(id, request);
        return Ok(result);
    }

    /// <summary>Huỷ hợp đồng khi chưa confirmed (drafted/pending_otp → cancelled).</summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var result = await _contractService.CancelAsync(id);
        return Ok(result);
    }
}
