using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IDesignService
{
    Task<PaginationResponse<DesignResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectProviderId = null, string? status = null, string? type = null);
    Task<DesignResponse> GetByIdAsync(long id);
    Task<DesignResponse> CreateAsync(CreateDesignRequest request);
    Task<DesignResponse> UpdateAsync(long id, UpdateDesignRequest request);

    // Vòng duyệt/revision: in_progress → submitted → approved | revision → in_progress → submitted…
    Task<DesignResponse> SubmitAsync(long id);
    Task<DesignResponse> ApproveAsync(long id);
    Task<DesignResponse> RequestRevisionAsync(long id, RequestDesignRevisionRequest request);
    Task<DesignResponse> StartRevisionAsync(long id);

    // Ảnh của design
    Task<DesignImageResponse> AddImageAsync(long designId, AddDesignImageRequest request);
    Task RemoveImageAsync(long designId, long imageId);
}
