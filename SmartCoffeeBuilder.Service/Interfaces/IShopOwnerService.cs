using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests;
using SmartCoffeeBuilder.Service.DTOs.Responses;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IShopOwnerService
{
    Task<PaginationResponse<ShopOwnerResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<ShopOwnerResponse> GetByIdAsync(long id);
    Task<ShopOwnerResponse> CreateAsync(CreateShopOwnerRequest request);
    Task<ShopOwnerResponse> UpdateAsync(long id, UpdateShopOwnerRequest request);
    Task DeleteAsync(long id);
}
