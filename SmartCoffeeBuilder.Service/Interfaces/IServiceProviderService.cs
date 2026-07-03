using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ServiceProvider;
using SmartCoffeeBuilder.Service.DTOs.Responses.ServiceProvider;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IServiceProviderService
{
    /// <summary>
    /// Tìm provider để thuê trực tiếp.
    /// capability: designer | constructor | both — truyền designer/constructor sẽ gồm cả provider "both".
    /// </summary>
    Task<PaginationResponse<ServiceProviderResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        string? capability = null, bool? isVerified = null, string? search = null);
    Task<ServiceProviderResponse> GetByIdAsync(long id);
    Task<ServiceProviderResponse> CreateAsync(CreateServiceProviderRequest request);
    Task<ServiceProviderResponse> UpdateAsync(long id, UpdateServiceProviderRequest request);
    Task DeleteAsync(long id);
}
