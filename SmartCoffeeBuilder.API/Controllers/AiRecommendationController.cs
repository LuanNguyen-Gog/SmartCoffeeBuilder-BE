using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/ai-recommendations")]
// Role gate nằm ở TỪNG endpoint, không đặt ở cấp class nữa: đọc và ghi có hai tập người dùng
// khác nhau. Trước đây cả controller khoá 'owner,admin' nên provider duyệt marketplace ăn 403
// ngay ở gate, không bao giờ tới được service — mà bài đăng vốn là lời mời thầu công khai,
// không xem được concept AI thì không đủ căn cứ nộp hồ sơ.
// Quyền theo từng bản ghi vẫn do service quyết (EnsureBriefVisibleAsync / EnsureBriefOwnerAsync);
// role gate ở đây chỉ là lớp phụ, KHÔNG thay được ownership check.
[Authorize]
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

    /// <summary>
    /// [XEM AI] Kết quả AI của một brief. Chủ dự án, provider có engagement còn hiệu lực, và
    /// mọi tài khoản khi dự án còn bài đăng 'open' (marketplace) đều đọc được — service lọc.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllByBriefId(
        [FromQuery, BindRequired] Guid briefId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _aiRecommendationService.GetAllByBriefIdAsync(
            User.GetAccountId(), briefId, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _aiRecommendationService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [CHẠY AI] Sinh concept cho brief. Chỉ chủ dự án (hoặc admin) — job tốn quota và ghi
    /// bản ghi vào brief, nên đây là đường GHI và giữ nguyên role gate cũ.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> GenerateDesign([FromBody] GenerateAiDesignRequest request)
    {
        var userId = GetUserId();
        var result = await _aiRecommendationService.GenerateDesignAsync(request.BriefId, userId, request);
        return Accepted(result);
    }
}
