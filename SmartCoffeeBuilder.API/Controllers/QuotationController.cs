using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Báo giá tiền hợp đồng (review 3): provider gửi bảng hạng mục + điều kiện thanh toán, owner so
/// sánh nhiều bản rồi duyệt một bản. Duyệt báo giá của một hồ sơ ứng tuyển ĐỒNG THỜI là chọn
/// provider đó cho dự án.
///
/// Vòng đời: draft → sent → accepted | rejected | revision_requested (provider phát hành bản mới).
/// </summary>
[ApiController]
[Route("api/quotations")]
[Authorize]
public class QuotationController : ControllerBase
{
    private readonly IQuotationService _quotationService;

    public QuotationController(IQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    /// <summary>
    /// Danh sách báo giá, đã lọc theo người đang đăng nhập: owner thấy báo giá gửi cho dự án mình,
    /// provider thấy báo giá mình gửi, admin thấy tất cả.
    /// Lọc <c>postId</c> để owner xem mọi báo giá của một bài đăng cạnh nhau mà so sánh.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? applyId = null,
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] Guid? postId = null,
        [FromQuery] string? status = null)
    {
        var result = await _quotationService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, applyId, projectWorkingId, postId, status);
        return Ok(result);
    }

    /// <summary>Chi tiết báo giá kèm hạng mục, điều kiện thanh toán và file đính kèm.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _quotationService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Provider lập bản báo giá mới (draft). Neo vào ĐÚNG MỘT trong hai: <c>applyId</c> (kèm hồ sơ
    /// ứng tuyển) hoặc <c>projectWorkingId</c> (owner đã mời trực tiếp).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "provider")]
    public async Task<IActionResult> Create([FromBody] CreateQuotationRequest request)
    {
        var result = await _quotationService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Sửa bản nháp (hạng mục / điều kiện thanh toán gửi lên là thay toàn bộ).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuotationRequest request)
    {
        var result = await _quotationService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Gửi báo giá cho chủ quán (draft → sent).</summary>
    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Send(Guid id)
    {
        var result = await _quotationService.SendAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Chủ quán yêu cầu provider gửi bản khác, kèm lý do (bắt buộc).</summary>
    [HttpPost("{id:guid}/request-revision")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] RespondQuotationRequest request)
    {
        var result = await _quotationService.RequestRevisionAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Chủ quán từ chối hẳn bản báo giá này.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RespondQuotationRequest request)
    {
        var result = await _quotationService.RejectAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Chủ quán duyệt báo giá — báo giá bị khoá lại và trở thành nguồn dựng hợp đồng.
    /// Với báo giá kèm hồ sơ ứng tuyển: hệ thống chấp nhận luôn hồ sơ đó, mở engagement, đóng bài
    /// đăng và cho các hồ sơ + báo giá còn lại hết hiệu lực (trả kèm engagement vừa tạo).
    /// </summary>
    // KHÔNG mở cho admin: duyệt báo giá là cam kết tiền bạc của chủ quán, không uỷ quyền được.
    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "owner")]
    public async Task<IActionResult> Accept(Guid id)
    {
        var result = await _quotationService.AcceptAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Đính kèm file (đã upload qua api/files) vào báo giá.</summary>
    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> AddAttachment(Guid id, [FromBody] AddQuotationAttachmentRequest request)
    {
        var result = await _quotationService.AddAttachmentAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveAttachment(Guid id, Guid attachmentId)
    {
        await _quotationService.RemoveAttachmentAsync(User.GetAccountId(), id, attachmentId);
        return NoContent();
    }

    /// <summary>Provider xoá bản nháp của mình (chỉ khi còn 'draft').</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _quotationService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
