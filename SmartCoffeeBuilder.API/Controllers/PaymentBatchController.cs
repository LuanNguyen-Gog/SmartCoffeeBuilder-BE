using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.PaymentBatch;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Đợt thanh toán owner → provider của một hợp đồng (review 3). Hệ thống KHÔNG giữ tiền: owner
/// chuyển khoản thẳng rồi upload minh chứng, provider xác nhận đã nhận.
///
/// Không có endpoint tạo đợt — đợt sinh tự động từ điều kiện thanh toán của báo giá lúc hợp đồng
/// được ký (POST /api/contracts/{id}/confirm-otp).
/// </summary>
[ApiController]
[Route("api/payment-batches")]
[Authorize]
public class PaymentBatchController : ControllerBase
{
    private readonly IPaymentBatchService _paymentBatchService;

    public PaymentBatchController(IPaymentBatchService paymentBatchService)
    {
        _paymentBatchService = paymentBatchService;
    }

    /// <summary>
    /// Danh sách đợt thanh toán, đã lọc theo người đang đăng nhập (owner của dự án / provider của
    /// engagement / admin). Lọc theo <c>contractId</c> hoặc <c>projectWorkingId</c>.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? contractId = null,
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] string? status = null)
    {
        var result = await _paymentBatchService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, contractId, projectWorkingId, status);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _paymentBatchService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Chủ quán nộp minh chứng đã chuyển tiền cho đợt này (ảnh upload trước qua api/files).
    /// Nộp được nhiều lần khi trả làm nhiều đợt nhỏ hoặc khi bản trước bị nhà cung cấp bác.
    /// </summary>
    [HttpPost("{id:guid}/proofs")]
    [Authorize(Roles = "owner")]
    public async Task<IActionResult> SubmitProof(Guid id, [FromBody] SubmitPaymentProofRequest request)
    {
        var result = await _paymentBatchService.SubmitProofAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Nhà cung cấp xác nhận đã nhận đủ tiền. Hạng mục thi công gắn với đợt này (nếu có) được đánh
    /// dấu đã thanh toán.
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "provider")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _paymentBatchService.ConfirmAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Nhà cung cấp bác minh chứng kèm lý do — chủ quán nộp lại.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "provider")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPaymentBatchRequest request)
    {
        var result = await _paymentBatchService.RejectAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Nhà cung cấp gắn đợt thanh toán vào một hạng mục thi công (gửi null để gỡ liên kết) —
    /// đây là chỗ nối "đã thanh toán" với "hạng mục nào" theo yêu cầu review 3.
    /// </summary>
    [HttpPut("{id:guid}/construction-item")]
    [Authorize(Roles = "provider")]
    public async Task<IActionResult> LinkConstructionItem(
        Guid id, [FromBody] LinkConstructionItemRequest request)
    {
        var result = await _paymentBatchService.LinkConstructionItemAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }
}
