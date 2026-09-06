using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ShopOwner;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IShopOwnerService
{
    Task<PaginationResponse<ShopOwnerResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<ShopOwnerResponse> GetByIdAsync(Guid id);
    /// <param name="accountId">Người đang đăng nhập — chỉ dựng hồ sơ cho chính account này.</param>
    Task<ShopOwnerResponse> CreateAsync(Guid accountId, CreateShopOwnerRequest request);
    /// <param name="accountId">Người đang đăng nhập — hồ sơ phải là của chính account này (admin đi cửa riêng).</param>
    Task<ShopOwnerResponse> UpdateAsync(Guid accountId, Guid id, UpdateShopOwnerRequest request);
    Task DeleteAsync(Guid id);
}
