using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.DesignBrief;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/design-briefs")]
[Authorize]
public class DesignBriefController : ControllerBase
{
    private readonly IDesignBriefService _designBriefService;

    public DesignBriefController(IDesignBriefService designBriefService)
    {
        _designBriefService = designBriefService;
    }

    /// <summary>
    /// Danh sách brief người gọi được xem: chủ dự án thấy brief dự án mình, provider thấy brief của
    /// dự án đang hợp tác hoặc đang mở thầu, admin thấy tất cả.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? projectShopOwnerId = null)
    {
        var result = await _designBriefService.GetAllAsync(User.GetAccountId(), pageNumber, pageSize, projectShopOwnerId);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _designBriefService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Create([FromBody] CreateDesignBriefRequest request)
    {
        var result = await _designBriefService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDesignBriefRequest request)
    {
        var result = await _designBriefService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _designBriefService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }
}
