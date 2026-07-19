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
    Task<ServiceProviderProfileResponse> GetByIdAsync(long id);
    Task<ServiceProviderProfileResponse> CreateAsync(CreateServiceProviderProfileRequest request);
    Task<ServiceProviderProfileResponse> UpdateAsync(long id, UpdateServiceProviderProfileRequest request);
    Task DeleteAsync(long id);
}
