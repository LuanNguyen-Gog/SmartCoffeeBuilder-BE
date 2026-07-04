using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProvider;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/service-providers")]
[Authorize]
public class ServiceProviderController : ControllerBase
{
    private readonly IServiceProviderService _serviceProviderService;

    public ServiceProviderController(IServiceProviderService serviceProviderService)
    {
        _serviceProviderService = serviceProviderService;
    }

    /// <summary>
    /// [TÌM NGƯỜI] Owner tìm provider để thuê trực tiếp.
    /// capability: designer | constructor | both (lọc designer/constructor tự gồm cả "both").
    /// Kết quả xếp theo rating giảm dần.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? capability = null,
        [FromQuery] bool? isVerified = null,
        [FromQuery] string? search = null)
    {
        var result = await _serviceProviderService.GetAllAsync(pageNumber, pageSize, capability, isVerified, search);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _serviceProviderService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceProviderRequest request)
    {
        var result = await _serviceProviderService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateServiceProviderRequest request)
    {
        var result = await _serviceProviderService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        await _serviceProviderService.DeleteAsync(id);
        return NoContent();
    }
}
