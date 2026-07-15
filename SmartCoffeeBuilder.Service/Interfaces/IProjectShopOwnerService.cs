using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectShopOwner;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectShopOwnerService
{
    Task<PaginationResponse<ProjectShopOwnerResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? ownerId = null);
    Task<ProjectShopOwnerResponse> GetByIdAsync(long id);
    Task<ProjectShopOwnerResponse> CreateAsync(CreateProjectShopOwnerRequest request);
    Task<ProjectShopOwnerResponse> UpdateAsync(long id, UpdateProjectShopOwnerRequest request);
    Task DeleteAsync(long id);
}
