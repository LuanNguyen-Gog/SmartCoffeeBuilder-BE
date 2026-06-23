using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests;
using SmartCoffeeBuilder.Service.DTOs.Responses;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IServiceProviderService
{
    Task<PaginationResponse<ServiceProviderResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<ServiceProviderResponse> GetByIdAsync(long id);
    Task<ServiceProviderResponse> CreateAsync(CreateServiceProviderRequest request);
    Task<ServiceProviderResponse> UpdateAsync(long id, UpdateServiceProviderRequest request);
    Task DeleteAsync(long id);
}
