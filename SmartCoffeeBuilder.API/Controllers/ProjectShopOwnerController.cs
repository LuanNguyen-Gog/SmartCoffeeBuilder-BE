using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/project-shop-owners")]
[Authorize]
public class ProjectShopOwnerController : ControllerBase
{
    private readonly IProjectShopOwnerService _projectShopOwnerService;

    public ProjectShopOwnerController(IProjectShopOwnerService projectShopOwnerService)
    {
        _projectShopOwnerService = projectShopOwnerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? ownerId = null)
    {
        var result = await _projectShopOwnerService.GetAllAsync(pageNumber, pageSize, ownerId);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _projectShopOwnerService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectShopOwnerRequest request)
    {
        var result = await _projectShopOwnerService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Sửa thông tin dự án. Trường status chỉ nhận transition thường (briefed → in_progress);
    /// muốn đóng dự án thì dùng /complete hoặc /cancel bên dưới.
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectShopOwnerRequest request)
    {
        var result = await _projectShopOwnerService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// [OWNER — ĐÓNG DỰ ÁN] Hoàn thành dự án (in_progress → completed).
    /// Điều kiện: không còn engagement 'requested'/'accepted', và có ít nhất một engagement
    /// đã nghiệm thu ('completed'). Các bài đăng còn 'open' được đóng theo.
    /// </summary>
    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id)
    {
        var result = await _projectShopOwnerService.CompleteAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [OWNER — HUỶ DỰ ÁN] briefed/in_progress → cancelled. Engagement đang mở bị đóng theo
    /// (requested → rejected, accepted → terminated) và bài đăng 'open' → 'closed'.
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id)
    {
        var result = await _projectShopOwnerService.CancelAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _projectShopOwnerService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
