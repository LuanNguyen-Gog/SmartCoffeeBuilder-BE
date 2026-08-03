using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectWorkingService
{
    Task<PaginationResponse<ProjectWorkingResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectShopOwnerId = null, long? serviceProviderProfileId = null, string? status = null);

    Task<ProjectWorkingResponse> GetByIdAsync(long id);

    /// <summary>Owner gửi lời mời thuê trực tiếp — engagement tạo với status=requested (application_id=null).</summary>
    Task<ProjectWorkingResponse> CreateDirectRequestAsync(CreateProjectWorkingRequest request);

    /// <summary>[PROVIDER] Chấp nhận lời mời (requested → accepted).</summary>
    Task<ProjectWorkingResponse> AcceptAsync(long accountId, long id);

    /// <summary>[PROVIDER] Từ chối lời mời (requested → rejected).</summary>
    Task<ProjectWorkingResponse> RejectAsync(long accountId, long id);

    /// <summary>
    /// [PROVIDER] Báo đã xong phần việc, xin owner nghiệm thu.
    /// KHÔNG đổi provider_status (vẫn 'accepted') — chỉ đặt mốc completion_requested_at.
    /// Yêu cầu: engagement 'accepted', có contract 'confirmed', và deliverable đã xong
    /// (design: có ít nhất 1 bản 'approved'; construction: mọi milestone 'completed').
    /// </summary>
    Task<ProjectWorkingResponse> RequestCompletionAsync(
        long accountId, long id, RequestEngagementCompletionRequest request);

    /// <summary>[OWNER] Nghiệm thu engagement (accepted → completed) — mở khoá review. Cần contract 'confirmed'.</summary>
    Task<ProjectWorkingResponse> CompleteAsync(long accountId, long id);

    /// <summary>[OWNER hoặc PROVIDER] Huỷ ngang engagement đang chạy (accepted → terminated).</summary>
    Task<ProjectWorkingResponse> TerminateAsync(long accountId, long id);

    /// <summary>
    /// Chuyển trạng thái QUAN HỆ của engagement (v5) — endpoint tổng, giữ cho tương thích ngược.
    /// Uỷ quyền về đúng method chuyên dụng ở trên nên áp dụng cùng bộ kiểm tra quyền:
    /// accepted/rejected = provider; completed = owner; terminated = một trong hai bên.
    /// Tiến độ design/construction là derived, không đi qua đây.
    /// </summary>
    Task<ProjectWorkingResponse> UpdateStatusAsync(long accountId, long id, UpdateProjectWorkingStatusRequest request);

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
