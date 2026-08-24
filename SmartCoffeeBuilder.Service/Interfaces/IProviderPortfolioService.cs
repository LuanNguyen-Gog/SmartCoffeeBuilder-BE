using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProviderPortfolio;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProviderPortfolio;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Dự án mẫu trong hồ sơ năng lực nhà cung cấp (review 1.1). Danh sách là CÔNG KHAI — chủ quán
/// phải xem được trước khi quyết định thuê; chỉ chính provider (hoặc admin) mới ghi.
/// </summary>
public interface IProviderPortfolioService
{
    Task<PaginationResponse<ProviderPortfolioResponse>> GetByProviderAsync(
        Guid serviceProviderProfileId, int pageNumber = 1, int pageSize = 20);

    Task<ProviderPortfolioResponse> GetByIdAsync(Guid id);

    Task<ProviderPortfolioResponse> CreateAsync(Guid accountId, CreateProviderPortfolioRequest request);
    Task<ProviderPortfolioResponse> UpdateAsync(Guid accountId, Guid id, UpdateProviderPortfolioRequest request);
    Task DeleteAsync(Guid accountId, Guid id);

    Task<ProviderPortfolioImageResponse> AddImageAsync(
        Guid accountId, Guid portfolioId, ProviderPortfolioImageRequest request);
    Task RemoveImageAsync(Guid accountId, Guid imageId);
}
