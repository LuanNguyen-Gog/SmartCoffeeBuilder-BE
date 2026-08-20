using SmartCoffeeBuilder.Service.DTOs.Requests.SiteProfile;
using SmartCoffeeBuilder.Service.DTOs.Responses.SiteProfile;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Hồ sơ thông số vật lý của mặt bằng (review 1.1). Chủ dự án và provider đang hợp tác cùng ghi
/// được — số đo thật thường do provider điền sau khi đi khảo sát.
/// </summary>
public interface ISiteProfileService
{
    /// <summary>Hồ sơ mặt bằng của một dự án. Ném 404 khi dự án chưa khai.</summary>
    Task<SiteProfileResponse> GetByProjectAsync(Guid accountId, Guid projectShopOwnerId);

    Task<SiteProfileResponse> GetByIdAsync(Guid accountId, Guid id);
    Task<SiteProfileResponse> CreateAsync(Guid accountId, CreateSiteProfileRequest request);
    Task<SiteProfileResponse> UpdateAsync(Guid accountId, Guid id, UpdateSiteProfileRequest request);
    Task DeleteAsync(Guid accountId, Guid id);

    // ── Tầng ───────────────────────────────────────────────────────────────
    Task<SiteFloorResponse> AddFloorAsync(Guid accountId, Guid siteProfileId, SiteFloorRequest request);
    Task<SiteFloorResponse> UpdateFloorAsync(Guid accountId, Guid floorId, SiteFloorRequest request);
    Task RemoveFloorAsync(Guid accountId, Guid floorId);

    // ── Cửa / ban công ─────────────────────────────────────────────────────
    Task<SiteOpeningResponse> AddOpeningAsync(Guid accountId, Guid siteProfileId, SiteOpeningRequest request);
    Task<SiteOpeningResponse> UpdateOpeningAsync(Guid accountId, Guid openingId, SiteOpeningRequest request);
    Task RemoveOpeningAsync(Guid accountId, Guid openingId);
}
