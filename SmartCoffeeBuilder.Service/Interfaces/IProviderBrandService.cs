using SmartCoffeeBuilder.Service.DTOs.Requests.ProviderBrand;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProviderBrand;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Thương hiệu + năng lực của nhà cung cấp (review 1.1): nhận diện, kênh mạng xã hội, khu vực phục
/// vụ, giấy phép/chứng chỉ.
///
/// ĐỌC công khai với tài khoản đã đăng nhập; GHI chỉ chính chủ hồ sơ hoặc admin — mọi method ghi
/// đều nhận <c>accountId</c> lấy từ JWT.
/// </summary>
public interface IProviderBrandService
{
    Task<ProviderBrandResponse> GetAsync(Guid serviceProviderProfileId);

    Task<ProviderBrandResponse> UpdateBrandAsync(
        Guid accountId, Guid serviceProviderProfileId, UpdateProviderBrandRequest request);

    // ── Kênh thương hiệu ───────────────────────────────────────────────────
    Task<ProviderSocialLinkResponse> AddSocialLinkAsync(
        Guid accountId, Guid serviceProviderProfileId, ProviderSocialLinkRequest request);
    Task<ProviderSocialLinkResponse> UpdateSocialLinkAsync(
        Guid accountId, Guid linkId, ProviderSocialLinkRequest request);
    Task RemoveSocialLinkAsync(Guid accountId, Guid linkId);

    // ── Khu vực phục vụ ────────────────────────────────────────────────────
    Task<ProviderServiceAreaResponse> AddServiceAreaAsync(
        Guid accountId, Guid serviceProviderProfileId, ProviderServiceAreaRequest request);
    Task RemoveServiceAreaAsync(Guid accountId, Guid areaId);

    // ── Giấy phép / chứng chỉ ──────────────────────────────────────────────
    Task<ProviderCertificateResponse> AddCertificateAsync(
        Guid accountId, Guid serviceProviderProfileId, ProviderCertificateRequest request);
    Task<ProviderCertificateResponse> UpdateCertificateAsync(
        Guid accountId, Guid certificateId, ProviderCertificateRequest request);
    Task RemoveCertificateAsync(Guid accountId, Guid certificateId);

    /// <summary>Admin đối chiếu bản gốc và đánh dấu một giấy tờ là đã xác minh.</summary>
    Task<ProviderCertificateResponse> VerifyCertificateAsync(
        Guid accountId, Guid certificateId, bool isVerified);
}
