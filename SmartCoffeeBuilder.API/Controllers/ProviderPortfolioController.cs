using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProviderPortfolio;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Dự án mẫu trong hồ sơ năng lực nhà cung cấp (review 1.1: "mở rộng hồ sơ provider bằng dự án
/// mẫu, video, năng lực…").
///
/// ĐỌC mở cho mọi tài khoản đã đăng nhập — chủ quán phải xem được công trình cũ trước khi thuê.
/// GHI có role gate <c>provider,admin</c>, nhưng quyền thật (hồ sơ này có phải của bạn không)
/// nằm ở <c>ProviderPortfolioService.EnsureCanWriteAsync</c>.
/// </summary>
[ApiController]
[Route("api/provider-portfolios")]
[Authorize]
public class ProviderPortfolioController : ControllerBase
{
    private readonly IProviderPortfolioService _providerPortfolioService;

    public ProviderPortfolioController(IProviderPortfolioService providerPortfolioService)
    {
        _providerPortfolioService = providerPortfolioService;
    }

    /// <summary>Dự án mẫu của một nhà cung cấp — ghim lên đầu, rồi tới thứ tự tự sắp.</summary>
    [HttpGet]
    public async Task<IActionResult> GetByProvider(
        [FromQuery] Guid serviceProviderProfileId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _providerPortfolioService.GetByProviderAsync(
            serviceProviderProfileId, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _providerPortfolioService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Thêm dự án mẫu. Bỏ trống <c>serviceProviderProfileId</c> để gắn vào hồ sơ của chính mình.
    /// Ảnh/video phải upload qua <c>/api/files</c> trước rồi gửi giá trị trả về.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateProviderPortfolioRequest request)
    {
        var result = await _providerPortfolioService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProviderPortfolioRequest request)
    {
        var result = await _providerPortfolioService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>Xoá dự án mẫu — ảnh con và file trên bucket dọn theo.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _providerPortfolioService.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> AddImage(Guid id, [FromBody] ProviderPortfolioImageRequest request)
    {
        var result = await _providerPortfolioService.AddImageAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    [HttpDelete("images/{imageId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveImage(Guid imageId)
    {
        await _providerPortfolioService.RemoveImageAsync(User.GetAccountId(), imageId);
        return NoContent();
    }
}
