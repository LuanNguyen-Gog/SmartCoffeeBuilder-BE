using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using SmartCoffeeBuilder.Service.DTOs.Requests.Material;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Vật tư thi công (review 3): bảng giá công bố TRƯỚC khi làm, rồi từng hạng mục / task chọn ra
/// và khai khối lượng dự tính; khối lượng thực tế điền sau khi thi công.
///
/// Nhà cung cấp khai; chủ quán đọc.
/// </summary>
[ApiController]
[Route("api/materials")]
[Authorize]
public class MaterialController : ControllerBase
{
    private readonly IMaterialService _materialService;

    public MaterialController(IMaterialService materialService)
    {
        _materialService = materialService;
    }

    // ───────────────────────── Bảng giá ─────────────────────────

    /// <summary>Bảng giá vật tư đã công bố cho một hợp tác.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery, BindRequired] Guid projectWorkingId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await _materialService.GetAllAsync(
            User.GetAccountId(), projectWorkingId, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _materialService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Thêm một vật tư vào bảng giá (yêu cầu hợp tác đã có hợp đồng ký).</summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateMaterialRequest request)
    {
        var result = await _materialService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMaterialRequest request)
    {
        var result = await _materialService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Xoá khỏi bảng giá — chặn khi vẫn còn hạng mục/task đang dùng.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _materialService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }

    // ───────────────────────── Lượng dùng ─────────────────────────

    /// <summary>Vật tư của một hạng mục hoặc một task (gửi đúng một trong hai tham số).</summary>
    [HttpGet("usages")]
    public async Task<IActionResult> GetUsages(
        [FromQuery] Guid? constructionItemId = null,
        [FromQuery] Guid? constructionTaskId = null)
    {
        var result = await _materialService.GetUsagesAsync(
            User.GetAccountId(), constructionItemId, constructionTaskId);
        return Ok(result);
    }

    /// <summary>Chọn một vật tư từ bảng giá cho hạng mục/task kèm lượng dự tính.</summary>
    [HttpPost("usages")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> AddUsage([FromBody] CreateConstructionMaterialRequest request)
    {
        var result = await _materialService.AddUsageAsync(User.GetAccountId(), request);
        return Ok(result);
    }

    /// <summary>Sửa lượng dự tính, hoặc ghi lượng THỰC TẾ sau khi đã thi công.</summary>
    [HttpPut("usages/{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateUsage(
        Guid id, [FromBody] UpdateConstructionMaterialRequest request)
    {
        var result = await _materialService.UpdateUsageAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("usages/{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveUsage(Guid id)
    {
        await _materialService.RemoveUsageAsync(User.GetAccountId(), id);
        return NoContent();
    }

    /// <summary>
    /// Chi phí vật tư của một milestone: phần khai riêng + gộp từ mọi task con.
    /// </summary>
    [HttpGet("cost/construction-items/{constructionItemId:guid}")]
    public async Task<IActionResult> GetItemCost(Guid constructionItemId)
    {
        var result = await _materialService.GetItemCostAsync(User.GetAccountId(), constructionItemId);
        return Ok(result);
    }
}
