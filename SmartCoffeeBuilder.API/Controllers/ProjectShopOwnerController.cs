using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;
using SmartCoffeeBuilder.Service.Interfaces;

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

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProjectShopOwnerRequest request)
    {
        var result = await _projectShopOwnerService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _projectShopOwnerService.DeleteAsync(id);
        return NoContent();
    }
}
