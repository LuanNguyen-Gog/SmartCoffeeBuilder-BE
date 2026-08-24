using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.SiteProfile;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Hồ sơ thông số mặt bằng (review 1.1): kích thước, hướng, số tầng, cửa và ban công.
///
/// KHÔNG có role gate <c>[Authorize(Roles=)]</c> ở đây — cả chủ quán lẫn nhà cung cấp đang thực
/// hiện dự án đều ghi được (số đo thật thường do provider điền sau khảo sát). Quyền thật nằm ở
/// <c>SiteProfileService.EnsureCanWriteAsync</c>, chỗ duy nhất phân biệt được "dự án NÀO của ai".
/// </summary>
[ApiController]
[Route("api/site-profiles")]
[Authorize]
public class SiteProfileController : ControllerBase
{
    private readonly ISiteProfileService _siteProfileService;

    public SiteProfileController(ISiteProfileService siteProfileService)
    {
        _siteProfileService = siteProfileService;
    }

    /// <summary>Hồ sơ mặt bằng của một dự án (1-1). 404 khi dự án chưa khai.</summary>
    [HttpGet("by-project/{projectShopOwnerId:guid}")]
    public async Task<IActionResult> GetByProject(Guid projectShopOwnerId)
    {
        var result = await _siteProfileService.GetByProjectAsync(User.GetAccountId(), projectShopOwnerId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _siteProfileService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Khai hồ sơ mặt bằng. Gửi kèm <c>floors</c> / <c>openings</c> để tạo một lượt; ô cửa tham
    /// chiếu tầng bằng <c>floorNo</c> vì id tầng chưa tồn tại lúc gửi.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSiteProfileRequest request)
    {
        var result = await _siteProfileService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSiteProfileRequest request)
    {
        var result = await _siteProfileService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Xoá hồ sơ — tầng và ô cửa cascade theo.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _siteProfileService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }

    /// <summary>
    /// [CHỦ DỰ ÁN] Duyệt số đo đã khảo sát → ghi tổng diện tích sàn vào <c>projects.area_m2</c>.
    ///
    /// Đây là bước chốt: trước khi bấm, dự án (và payload AI) vẫn dùng con số owner khai lúc lập
    /// dự án. Trả về hồ sơ đã cập nhật, trong đó <c>isAreaSyncedToProject</c> chuyển thành true.
    ///
    /// 401 nếu người gọi không phải chủ dự án — provider ghi được số đo nhưng không tự duyệt.
    /// 409 nếu chưa tầng nào khai diện tích, hoặc dự án đã completed/cancelled.
    /// </summary>
    [HttpPost("{id:guid}/approve-measurements")]
    public async Task<IActionResult> ApproveMeasurements(Guid id)
    {
        var result = await _siteProfileService.ApproveMeasurementsAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    // ───────────────────────── Tầng ─────────────────────────

    /// <summary>Thêm một tầng. <c>floorNo</c>: 1 = trệt, số âm = hầm, 0 = gác lửng.</summary>
    [HttpPost("{id:guid}/floors")]
    public async Task<IActionResult> AddFloor(Guid id, [FromBody] SiteFloorRequest request)
    {
        var result = await _siteProfileService.AddFloorAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpPut("floors/{floorId:guid}")]
    public async Task<IActionResult> UpdateFloor(Guid floorId, [FromBody] SiteFloorRequest request)
    {
        var result = await _siteProfileService.UpdateFloorAsync(User.GetAccountId(), floorId, request);
        return Ok(result);
    }

    /// <summary>Xoá tầng — ô cửa đã gán vào tầng này KHÔNG mất, chỉ bỏ liên kết tầng.</summary>
    [HttpDelete("floors/{floorId:guid}")]
    public async Task<IActionResult> RemoveFloor(Guid floorId)
    {
        await _siteProfileService.RemoveFloorAsync(User.GetAccountId(), floorId);
        return NoContent();
    }

    // ───────────────────────── Cửa / ban công ─────────────────────────

    /// <summary>Thêm một ô mở: cửa chính/phụ/phục vụ, cửa sổ, ban công, sân thượng, giếng trời.</summary>
    [HttpPost("{id:guid}/openings")]
    public async Task<IActionResult> AddOpening(Guid id, [FromBody] SiteOpeningRequest request)
    {
        var result = await _siteProfileService.AddOpeningAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpPut("openings/{openingId:guid}")]
    public async Task<IActionResult> UpdateOpening(Guid openingId, [FromBody] SiteOpeningRequest request)
    {
        var result = await _siteProfileService.UpdateOpeningAsync(User.GetAccountId(), openingId, request);
        return Ok(result);
    }

    [HttpDelete("openings/{openingId:guid}")]
    public async Task<IActionResult> RemoveOpening(Guid openingId)
    {
        await _siteProfileService.RemoveOpeningAsync(User.GetAccountId(), openingId);
        return NoContent();
    }
}
