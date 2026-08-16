using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Contract;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

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

    /// <summary>
    /// Danh sách hợp đồng, lọc theo engagement. Kết quả đã giới hạn theo người đang đăng nhập:
    /// owner thấy hợp đồng dự án mình, provider thấy hợp đồng engagement mình, admin thấy tất cả.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? projectWorkingId = null)
    {
        var result = await _contractService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, projectWorkingId);
        return Ok(result);
    }

    /// <summary>
    /// Chi tiết hợp đồng — chỉ hai bên của chính engagement đó (hoặc admin) đọc được, 401 nếu không.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _contractService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Provider của engagement tạo bản hợp đồng (draft) cho engagement 'accepted'.
    /// Owner hay provider khác gọi → 401.
    /// MỖI provider chỉ giữ được 1 hợp đồng còn hiệu lực với 1 dự án tại một thời điểm:
    /// đang có bản 'drafted' hoặc 'pending_otp' thì phải chờ ký xong hoặc huỷ bản đó
    /// (POST /{id}/cancel) trước; đã có bản 'confirmed' thì không lập thêm. Vi phạm → 409.
    /// </summary>
    // KHÔNG mở cho admin: service resolve provider của engagement từ token, admin vào cũng chỉ
    // soạn hộ được hợp đồng của người khác — không phải việc quản trị.
    [HttpPost]
    [Authorize(Roles = "provider")]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        var result = await _contractService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Cập nhật nội dung hợp đồng — chỉ provider của engagement, và chỉ khi còn 'drafted'.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContractRequest request)
    {
        var result = await _contractService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Owner của dự án tự yêu cầu phát OTP ký hợp đồng về email của mình (drafted → pending_otp).
    /// Bấm lại khi mã cũ CÒN HẠN → không gửi thêm mail, trả nguyên trạng thái hiện tại;
    /// chỉ khi mã đã hết hạn mới cấp mã mới. Provider KHÔNG gọi được endpoint này.
    /// </summary>
    // Chỉ owner — KHÔNG mở cho admin, giống confirm-otp: cả lượt ký hợp đồng không uỷ quyền được
    // (service dùng EnsureOwnerOfEngagement chứ không phải EnsureActor).
    [HttpPost("{id:guid}/send-otp")]
    [Authorize(Roles = "owner")]
    public async Task<IActionResult> SendOtp(Guid id)
    {
        var result = await _contractService.SendOtpAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Owner xác nhận OTP ký hợp đồng (pending_otp → confirmed).
    /// Người ký lấy từ token — chỉ owner của chính dự án mới gọi được.
    /// </summary>
    // Chỉ owner — KHÔNG mở cho admin: chữ ký hợp đồng không uỷ quyền được
    // (service dùng EnsureOwnerOfEngagement chứ không phải EnsureActor).
    [HttpPost("{id:guid}/confirm-otp")]
    [Authorize(Roles = "owner")]
    public async Task<IActionResult> ConfirmOtp(Guid id, [FromBody] ConfirmContractOtpRequest request)
    {
        var result = await _contractService.ConfirmOtpAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Huỷ hợp đồng khi chưa confirmed (drafted/pending_otp → cancelled).
    /// Cả owner lẫn provider của engagement đều huỷ được; người ngoài → 401.
    /// Đây cũng là cách giải phóng "chỗ" để provider lập được hợp đồng mới cho dự án.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _contractService.CancelAsync(User.GetAccountId(), id);
        return Ok(result);
    }
}
