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
        [FromQuery] Guid? ownerId = null)
    {
        var result = await _projectShopOwnerService.GetAllAsync(User.GetAccountId(), pageNumber, pageSize, ownerId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _projectShopOwnerService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Create([FromBody] CreateProjectShopOwnerRequest request)
    {
        var result = await _projectShopOwnerService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Sửa thông tin dự án. Trường status chỉ nhận transition thường (briefed → in_progress);
    /// muốn đóng dự án thì dùng /complete hoặc /cancel bên dưới.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectShopOwnerRequest request)
    {
        var result = await _projectShopOwnerService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>
    /// [OWNER — ĐÓNG DỰ ÁN] Hoàn thành dự án (in_progress → completed).
    /// Điều kiện: không còn engagement 'requested'/'accepted', và có ít nhất một engagement
    /// đã nghiệm thu ('completed'). Các bài đăng còn 'open' được đóng theo.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var result = await _projectShopOwnerService.CompleteAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// [OWNER — HUỶ DỰ ÁN] briefed/in_progress → cancelled. Engagement đang mở bị đóng theo
    /// (requested → rejected, accepted → terminated) và bài đăng 'open' → 'closed'.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _projectShopOwnerService.CancelAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _projectShopOwnerService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
