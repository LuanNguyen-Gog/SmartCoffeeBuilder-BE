using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ShopOwner;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IShopOwnerService
{
    Task<PaginationResponse<ShopOwnerResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<ShopOwnerResponse> GetByIdAsync(Guid id);
    Task<ShopOwnerResponse> CreateAsync(CreateShopOwnerRequest request);
    Task<ShopOwnerResponse> UpdateAsync(Guid id, UpdateShopOwnerRequest request);
    Task DeleteAsync(Guid id);
}
