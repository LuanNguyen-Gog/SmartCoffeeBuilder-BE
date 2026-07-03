using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectProvider;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectProvider;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectProviderService
{
    Task<PaginationResponse<ProjectProviderResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectId = null, long? providerId = null, string? status = null);

    Task<ProjectProviderResponse> GetByIdAsync(long id);

    /// <summary>Owner thuê trực tiếp — engagement tạo với status=requested (application_id=null).</summary>
    Task<ProjectProviderResponse> CreateAsync(CreateProjectProviderRequest request);

    /// <summary>
    /// Chuyển trạng thái engagement theo máy trạng thái:
    /// requested→accepted/rejected; accepted→designing/constructing;
    /// designing→designed; designed→constructing (contract type both)/completed;
    /// constructing→constructed; constructed→completed; đang hoạt động→terminated.
    /// </summary>
    Task<ProjectProviderResponse> UpdateStatusAsync(long id, UpdateProjectProviderStatusRequest request);
}
