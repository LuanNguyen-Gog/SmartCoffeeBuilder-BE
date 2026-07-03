using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectApplication;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectApplication;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectProvider;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectApplicationService
{
    Task<PaginationResponse<ProjectApplicationResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? postId = null, long? providerId = null, string? status = null);

    Task<ProjectApplicationResponse> GetByIdAsync(long id);

    /// <summary>Provider nộp hồ sơ ứng tuyển vào bài đăng đang mở.</summary>
    Task<ProjectApplicationResponse> CreateAsync(CreateProjectApplicationRequest request);

    /// <summary>Provider sửa hồ sơ khi còn pending.</summary>
    Task<ProjectApplicationResponse> UpdateAsync(long id, UpdateProjectApplicationRequest request);

    /// <summary>
    /// Owner chấp nhận hồ sơ: application→accepted, tạo project_provider (đường marketplace),
    /// đóng post, từ chối các hồ sơ pending còn lại.
    /// </summary>
    Task<ProjectProviderResponse> AcceptAsync(long id);

    /// <summary>Owner từ chối hồ sơ pending.</summary>
    Task<ProjectApplicationResponse> RejectAsync(long id);

    /// <summary>Provider rút hồ sơ khi còn pending.</summary>
    Task DeleteAsync(long id);
}
