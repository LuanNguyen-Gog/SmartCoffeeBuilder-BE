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

    /// <summary>
    /// [OWNER hoặc PROVIDER] Đề nghị huỷ ngang hợp tác đang chạy.
    /// KHÔNG đổi provider_status — engagement vẫn 'accepted' và chỉ chuyển 'terminated' khi
    /// bên còn lại bấm đồng ý (<see cref="RespondTerminationAsync"/>). Bên kia nhận noti + email.
    /// Yêu cầu: engagement 'accepted' và chưa có đề nghị nào đang treo.
    /// </summary>
    Task<ProjectWorkingResponse> RequestTerminationAsync(
        long accountId, long id, RequestEngagementTerminationRequest request);

    /// <summary>
    /// [BÊN CÒN LẠI] Phản hồi đề nghị huỷ ngang: đồng ý → accepted → terminated;
    /// từ chối → xoá đề nghị, engagement giữ 'accepted'. Bên đề nghị nhận noti + email.
    /// Chỉ bên KHÔNG gửi đề nghị mới phản hồi được (admin được can thiệp).
    /// </summary>
    Task<ProjectWorkingResponse> RespondTerminationAsync(
        long accountId, long id, RespondEngagementTerminationRequest request);

    /// <summary>
    /// [BÊN ĐỀ NGHỊ] Rút lại đề nghị huỷ ngang của chính mình khi bên kia chưa phản hồi.
    /// Bên còn lại nhận noti + email báo không cần phản hồi nữa.
    /// </summary>
    Task<ProjectWorkingResponse> CancelTerminationRequestAsync(long accountId, long id);

    /// <summary>
    /// [OWNER hoặc PROVIDER] Cửa vào huỷ ngang một chạm, giữ tương thích ngược cho FE cũ.
    /// KHÔNG còn huỷ thẳng: chưa có đề nghị → tạo đề nghị (engagement vẫn 'accepted');
    /// bên kia đã đề nghị → coi như đồng ý và chuyển 'terminated'.
    /// Admin gọi thì huỷ ngay (can thiệp hành chính, không cần đồng thuận).
    /// </summary>
    Task<ProjectWorkingResponse> TerminateAsync(long accountId, long id);

    /// <summary>
    /// Chuyển trạng thái QUAN HỆ của engagement (v5) — endpoint tổng, giữ cho tương thích ngược.
    /// Uỷ quyền về đúng method chuyên dụng ở trên nên áp dụng cùng bộ kiểm tra quyền:
    /// accepted/rejected = provider; completed = owner; terminated = đi qua luồng đồng thuận hai bên.
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
