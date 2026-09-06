using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ShopOwner;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/shop-owners")]
[Authorize]
public class ShopOwnerController : ControllerBase
{
    private readonly IShopOwnerService _shopOwnerService;

    public ShopOwnerController(IShopOwnerService shopOwnerService)
    {
        _shopOwnerService = shopOwnerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _shopOwnerService.GetAllAsync(pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _shopOwnerService.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Create([FromBody] CreateShopOwnerRequest request)
    {
        var result = await _shopOwnerService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateShopOwnerRequest request)
    {
        var result = await _shopOwnerService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _shopOwnerService.DeleteAsync(id);
        return NoContent();
    }
}
