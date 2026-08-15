using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/ai-recommendations")]
// Bước AI thuộc luồng brief của owner. Provider xem kết quả AI qua
// GET api/project-workings/{id}/overview (đã lọc theo engagement), không qua controller này.
[Authorize(Roles = "owner,admin")]
public class AiRecommendationController : ControllerBase
{
    private readonly IAiRecommendationService _aiRecommendationService;

    public AiRecommendationController(IAiRecommendationService aiRecommendationService)
    {
        _aiRecommendationService = aiRecommendationService;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpGet]
    public async Task<IActionResult> GetAllByBriefId(
        [FromQuery] long briefId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _aiRecommendationService.GetAllByBriefIdAsync(
            User.GetAccountId(), briefId, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _aiRecommendationService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateDesign([FromBody] GenerateAiDesignRequest request)
    {
        var userId = GetUserId();
        var result = await _aiRecommendationService.GenerateDesignAsync(request.BriefId, userId, request);
        return Accepted(result);
    }
}
