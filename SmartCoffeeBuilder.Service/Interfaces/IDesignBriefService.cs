using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.DesignBrief;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IDesignBriefService
{
    Task<PaginationResponse<DesignBriefResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? projectShopOwnerId = null);
    Task<DesignBriefResponse> GetByIdAsync(long id);
    Task<DesignBriefResponse> CreateAsync(CreateDesignBriefRequest request);
    Task<DesignBriefResponse> UpdateAsync(long id, UpdateDesignBriefRequest request);
    Task DeleteAsync(long id);
}
