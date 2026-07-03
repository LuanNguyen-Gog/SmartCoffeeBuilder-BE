using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectPost;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectPost;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectPostService
{
    /// <summary>
    /// Tìm/duyệt bài đăng. Provider thường lọc status=open, serviceKind theo capability.
    /// </summary>
    Task<PaginationResponse<ProjectPostResponse>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        long? projectId = null,
        string? serviceKind = null,
        string? status = null,
        string? search = null);

    Task<ProjectPostResponse> GetByIdAsync(long id);
    Task<ProjectPostResponse> CreateAsync(CreateProjectPostRequest request);
    Task<ProjectPostResponse> UpdateAsync(long id, UpdateProjectPostRequest request);
    Task DeleteAsync(long id);
}
