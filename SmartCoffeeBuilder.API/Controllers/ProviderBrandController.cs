using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProviderBrand;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Thương hiệu và năng lực nhà cung cấp (review 1.1): logo, ảnh bìa, video giới thiệu, website,
/// câu chuyện thương hiệu, kênh mạng xã hội, khu vực phục vụ, giấy phép/chứng chỉ.
///
/// ĐỌC mở cho mọi tài khoản đã đăng nhập — chủ quán phải xem được trước khi thuê.
/// GHI có role gate <c>provider,admin</c>, nhưng quyền thật (hồ sơ này có phải của bạn không)
/// nằm ở <c>ProviderBrandService.EnsureCanWriteAsync</c>.
/// </summary>
[ApiController]
[Route("api/provider-brands")]
[Authorize]
public class ProviderBrandController : ControllerBase
{
    private readonly IProviderBrandService _providerBrandService;

    public ProviderBrandController(IProviderBrandService providerBrandService)
    {
        _providerBrandService = providerBrandService;
    }

    /// <summary>Toàn bộ phần thương hiệu + năng lực của một hồ sơ, gộp một lần gọi.</summary>
    [HttpGet("{serviceProviderProfileId:guid}")]
    public async Task<IActionResult> Get(Guid serviceProviderProfileId)
    {
        var result = await _providerBrandService.GetAsync(serviceProviderProfileId);
        return Ok(result);
    }

    /// <summary>Cập nhật nhận diện thương hiệu. Ảnh/video upload qua /api/files trước.</summary>
    [HttpPut("{serviceProviderProfileId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateBrand(
        Guid serviceProviderProfileId, [FromBody] UpdateProviderBrandRequest request)
    {
        var result = await _providerBrandService.UpdateBrandAsync(
            User.GetAccountId(), serviceProviderProfileId, request);
        return Ok(result);
    }

    // ───────────────────────── Kênh thương hiệu ─────────────────────────

    /// <summary>Thêm kênh: facebook | instagram | tiktok | youtube | linkedin | zalo | website | other.</summary>
    [HttpPost("{serviceProviderProfileId:guid}/social-links")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> AddSocialLink(
        Guid serviceProviderProfileId, [FromBody] ProviderSocialLinkRequest request)
    {
        var result = await _providerBrandService.AddSocialLinkAsync(
            User.GetAccountId(), serviceProviderProfileId, request);
        return Ok(result);
    }

    [HttpPut("social-links/{linkId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateSocialLink(
        Guid linkId, [FromBody] ProviderSocialLinkRequest request)
    {
        var result = await _providerBrandService.UpdateSocialLinkAsync(
            User.GetAccountId(), linkId, request);
        return Ok(result);
    }

    [HttpDelete("social-links/{linkId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveSocialLink(Guid linkId)
    {
        await _providerBrandService.RemoveSocialLinkAsync(User.GetAccountId(), linkId);
        return NoContent();
    }

    // ───────────────────────── Khu vực phục vụ ─────────────────────────

    /// <summary>Thêm khu vực nhận việc. Bỏ trống <c>district</c> = nhận toàn tỉnh.</summary>
    [HttpPost("{serviceProviderProfileId:guid}/service-areas")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> AddServiceArea(
        Guid serviceProviderProfileId, [FromBody] ProviderServiceAreaRequest request)
    {
        var result = await _providerBrandService.AddServiceAreaAsync(
            User.GetAccountId(), serviceProviderProfileId, request);
        return Ok(result);
    }

    [HttpDelete("service-areas/{areaId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveServiceArea(Guid areaId)
    {
        await _providerBrandService.RemoveServiceAreaAsync(User.GetAccountId(), areaId);
        return NoContent();
    }

    // ───────────────────────── Giấy phép / chứng chỉ ─────────────────────────

    /// <summary>Thêm giấy tờ năng lực: license | certificate | award | membership | other.</summary>
    [HttpPost("{serviceProviderProfileId:guid}/certificates")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> AddCertificate(
        Guid serviceProviderProfileId, [FromBody] ProviderCertificateRequest request)
    {
        var result = await _providerBrandService.AddCertificateAsync(
            User.GetAccountId(), serviceProviderProfileId, request);
        return Ok(result);
    }

    /// <summary>Sửa giấy tờ — mọi lần sửa đều RESET cờ đã xác minh, admin phải duyệt lại.</summary>
    [HttpPut("certificates/{certificateId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UpdateCertificate(
        Guid certificateId, [FromBody] ProviderCertificateRequest request)
    {
        var result = await _providerBrandService.UpdateCertificateAsync(
            User.GetAccountId(), certificateId, request);
        return Ok(result);
    }

    [HttpDelete("certificates/{certificateId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveCertificate(Guid certificateId)
    {
        await _providerBrandService.RemoveCertificateAsync(User.GetAccountId(), certificateId);
        return NoContent();
    }

    /// <summary>Admin đối chiếu bản gốc và đánh dấu giấy tờ đã xác minh.</summary>
    [HttpPost("certificates/{certificateId:guid}/verify")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> VerifyCertificate(
        Guid certificateId, [FromQuery] bool isVerified = true)
    {
        var result = await _providerBrandService.VerifyCertificateAsync(
            User.GetAccountId(), certificateId, isVerified);
        return Ok(result);
    }
}
