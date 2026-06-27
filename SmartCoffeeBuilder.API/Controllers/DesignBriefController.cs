using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.DesignBrief;
using SmartCoffeeBuilder.Service.Interfaces;

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

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectId = null)
    {
        var result = await _designBriefService.GetAllAsync(pageNumber, pageSize, projectId);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _designBriefService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDesignBriefRequest request)
    {
        var result = await _designBriefService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDesignBriefRequest request)
    {
        var result = await _designBriefService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _designBriefService.DeleteAsync(id);
        return NoContent();
    }
}
