using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProviderProfile;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/service-provider-profiles")]
[Authorize]
public class ServiceProviderProfileController : ControllerBase
{
    private readonly IServiceProviderProfileService _serviceProviderProfileService;

    public ServiceProviderProfileController(IServiceProviderProfileService serviceProviderProfileService)
    {
        _serviceProviderProfileService = serviceProviderProfileService;
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
        var result = await _serviceProviderProfileService.GetAllAsync(pageNumber, pageSize, capability, isVerified, search);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _serviceProviderProfileService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateServiceProviderProfileRequest request)
    {
        var result = await _serviceProviderProfileService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceProviderProfileRequest request)
    {
        var result = await _serviceProviderProfileService.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _serviceProviderProfileService.DeleteAsync(id);
        return NoContent();
    }
}
