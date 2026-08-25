using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTemplate;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Mẫu quy trình thi công tái dùng (review 3): nhà cung cấp dựng sẵn bộ hạng mục + việc con kèm
/// thời lượng, rồi áp vào từng dự án thay vì gõ lại từ đầu.
///
/// Áp mẫu là COPY một lần — sửa mẫu về sau KHÔNG làm xê dịch dự án đã áp.
/// </summary>
[ApiController]
[Route("api/construction-templates")]
[Authorize]
public class ConstructionTemplateController : ControllerBase
{
    private readonly IConstructionTemplateService _constructionTemplateService;

    public ConstructionTemplateController(IConstructionTemplateService constructionTemplateService)
    {
        _constructionTemplateService = constructionTemplateService;
    }

    /// <summary>Mẫu công khai + mẫu riêng của chính người đang đăng nhập.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? serviceKind = null)
    {
        var result = await _constructionTemplateService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, serviceKind);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _constructionTemplateService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>Nhà cung cấp tạo mẫu mới (luôn là mẫu riêng khi tạo).</summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateConstructionTemplateRequest request)
    {
        var result = await _constructionTemplateService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Áp mẫu vào một hợp tác đã ký hợp đồng: sinh construction_item + construction_task, mốc
    /// estimate_at giãn dần theo thời lượng từng hạng mục kể từ <c>startDate</c>.
    /// </summary>
    [HttpPost("{id:guid}/apply")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Apply(Guid id, [FromBody] ApplyConstructionTemplateRequest request)
    {
        var result = await _constructionTemplateService.ApplyAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// Sắp lại thứ tự hạng mục trong mẫu (kéo thả trên FE). Gửi TOÀN BỘ id theo thứ tự mong muốn.
    /// Chỉ tác giả mẫu hoặc admin. Không ảnh hưởng dự án đã áp mẫu trước đó — áp mẫu là copy một lần.
    /// </summary>
    [HttpPut("{id:guid}/items/reorder")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> ReorderItems(
        Guid id, [FromBody] ReorderConstructionTemplateItemsRequest request)
    {
        var result = await _constructionTemplateService.ReorderItemsAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _constructionTemplateService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
