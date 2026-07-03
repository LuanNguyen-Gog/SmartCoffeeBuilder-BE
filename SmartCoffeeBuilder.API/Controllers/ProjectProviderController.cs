using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectProvider;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/project-providers")]
[Authorize]
public class ProjectProviderController : ControllerBase
{
    private readonly IProjectProviderService _projectProviderService;

    public ProjectProviderController(IProjectProviderService projectProviderService)
    {
        _projectProviderService = projectProviderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectId = null,
        [FromQuery] long? providerId = null,
        [FromQuery] string? status = null)
    {
        var result = await _projectProviderService.GetAllAsync(pageNumber, pageSize, projectId, providerId, status);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _projectProviderService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>Owner thuê provider trực tiếp (không qua marketplace).</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectProviderRequest request)
    {
        var result = await _projectProviderService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Chuyển trạng thái engagement (accept/reject/designing/…/completed/terminated).</summary>
    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateProjectProviderStatusRequest request)
    {
        var result = await _projectProviderService.UpdateStatusAsync(id, request);
        return Ok(result);
    }
}
