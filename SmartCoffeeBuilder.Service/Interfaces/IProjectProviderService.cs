using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectProvider;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectProvider;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectProviderService
{
    Task<PaginationResponse<ProjectProviderResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectId = null, long? providerId = null, string? status = null);

    Task<ProjectProviderResponse> GetByIdAsync(long id);

    /// <summary>Owner gửi lời mời thuê trực tiếp — engagement tạo với status=requested (application_id=null).</summary>
    Task<ProjectProviderResponse> CreateDirectRequestAsync(CreateProjectProviderRequest request);

    /// <summary>
    /// Chuyển trạng thái QUAN HỆ của engagement (v5):
    /// requested→accepted/rejected; accepted→completed (nghiệm thu, cần contract confirmed)/terminated.
    /// Tiến độ design/construction là derived, không đi qua đây.
    /// </summary>
    Task<ProjectProviderResponse> UpdateStatusAsync(long id, UpdateProjectProviderStatusRequest request);

    /// <summary>
    /// Provider xem brief của project để quyết định nhận việc — mở cho cả designer lẫn constructor,
    /// từ lúc được mời (requested) trở đi; engagement rejected/terminated không xem được.
    /// </summary>
    Task<DesignBriefResponse> GetBriefAsync(long id);

    /// <summary>
    /// Tổng quan dự án sau bước AI: engagement có design xem brief + AI plan;
    /// engagement chỉ construction xem bản vẽ 'approved' của bên design.
    /// </summary>
    Task<EngagementOverviewResponse> GetOverviewAsync(long id);
}
