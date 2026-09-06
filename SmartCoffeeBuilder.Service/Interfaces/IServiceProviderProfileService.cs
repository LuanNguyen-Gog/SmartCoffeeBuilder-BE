using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProviderProfile;
using SmartCoffeeBuilder.Service.DTOs.Responses.ServiceProviderProfile;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IServiceProviderProfileService
{
    /// <summary>
    /// Tìm provider để thuê trực tiếp.
    /// capability: designer | constructor | both — truyền designer/constructor sẽ gồm cả provider "both".
    /// </summary>
    Task<PaginationResponse<ServiceProviderProfileResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        string? capability = null, bool? isVerified = null, string? search = null);
    Task<ServiceProviderProfileResponse> GetByIdAsync(Guid id);
    /// <param name="accountId">Người đang đăng nhập — chỉ dựng hồ sơ cho chính account này.</param>
    Task<ServiceProviderProfileResponse> CreateAsync(
        Guid accountId, CreateServiceProviderProfileRequest request);
    /// <param name="accountId">
    /// Người đang đăng nhập. Hồ sơ phải là của chính account này; riêng <c>IsVerified</c>
    /// chỉ admin đổi được.
    /// </param>
    Task<ServiceProviderProfileResponse> UpdateAsync(
        Guid accountId, Guid id, UpdateServiceProviderProfileRequest request);
    Task DeleteAsync(Guid id);
}
