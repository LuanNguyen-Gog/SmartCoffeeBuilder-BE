using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Review;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Owner đánh giá provider sau khi nghiệm thu engagement (provider_status = 'completed').
/// Mỗi engagement (project_provider) chỉ có 1 review, kèm điểm theo từng tiêu chí (review_score).
/// </summary>
[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Danh sách review; lọc theo engagement hoặc provider.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] Guid? serviceProviderProfileId = null)
    {
        var result = await _reviewService.GetAllAsync(pageNumber, pageSize, projectWorkingId, serviceProviderProfileId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _reviewService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Tổng hợp rating của provider (điểm trung bình + theo tiêu chí) — cho trang profile.</summary>
    [HttpGet("providers/{serviceProviderProfileId:guid}/summary")]
    public async Task<IActionResult> GetProviderSummary(Guid serviceProviderProfileId)
    {
        var result = await _reviewService.GetProviderSummaryAsync(serviceProviderProfileId);
        return Ok(result);
    }

    /// <summary>Owner tạo review — engagement phải 'completed' và chưa có review.</summary>
    // KHÔNG mở cho admin: đánh giá là tiếng nói của chủ quán, không uỷ quyền được.
    [HttpPost]
    [Authorize(Roles = "owner")]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        var result = await _reviewService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReviewRequest request)
    {
        var result = await _reviewService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _reviewService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
    /// <summary>Nhà cung cấp trả lời công khai một đánh giá (review 1.1). Gọi lại là ghi đè.</summary>
    [HttpPost("{id:guid}/reply")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyReviewRequest request)
    {
        var result = await _reviewService.ReplyAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Nhà cung cấp gỡ phản hồi của mình.</summary>
    [HttpDelete("{id:guid}/reply")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveReply(Guid id)
    {
        var result = await _reviewService.RemoveReplyAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Chủ quán đính ảnh thành phẩm. Upload qua /api/files trước rồi gửi ObjectName.</summary>
    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> AddImage(Guid id, [FromBody] ReviewImageRequest request)
    {
        var result = await _reviewService.AddImageAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("images/{imageId:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> RemoveImage(Guid imageId)
    {
        await _reviewService.RemoveImageAsync(User.GetAccountId(), imageId);
        return NoContent();
    }

}
