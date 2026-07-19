using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Review;
using SmartCoffeeBuilder.Service.Interfaces;

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
        [FromQuery] long? projectWorkingId = null,
        [FromQuery] long? serviceProviderProfileId = null)
    {
        var result = await _reviewService.GetAllAsync(pageNumber, pageSize, projectWorkingId, serviceProviderProfileId);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _reviewService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Tổng hợp rating của provider (điểm trung bình + theo tiêu chí) — cho trang profile.</summary>
    [HttpGet("providers/{serviceProviderProfileId:long}/summary")]
    public async Task<IActionResult> GetProviderSummary(long serviceProviderProfileId)
    {
        var result = await _reviewService.GetProviderSummaryAsync(serviceProviderProfileId);
        return Ok(result);
    }

    /// <summary>Owner tạo review — engagement phải 'completed' và chưa có review.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        var result = await _reviewService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateReviewRequest request)
    {
        var result = await _reviewService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _reviewService.DeleteAsync(id);
        return NoContent();
    }
}
