using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectShopOwner;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectShopOwnerService
{
    Task<PaginationResponse<ProjectShopOwnerResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? ownerId = null);
    Task<ProjectShopOwnerResponse> GetByIdAsync(long id);
    Task<ProjectShopOwnerResponse> CreateAsync(CreateProjectShopOwnerRequest request);

    /// <summary>
    /// Sửa thông tin dự án. Status chỉ nhận transition thường (briefed → in_progress);
    /// đóng dự án (completed/cancelled) phải qua <see cref="CompleteAsync"/> / <see cref="CancelAsync"/>.
    /// </summary>
    Task<ProjectShopOwnerResponse> UpdateAsync(long accountId, long id, UpdateProjectShopOwnerRequest request);

    /// <summary>
    /// [OWNER — ĐÓNG DỰ ÁN] in_progress → completed. Yêu cầu mọi engagement đã đóng
    /// và có ít nhất một engagement được nghiệm thu. Post còn 'open' bị đóng theo.
    /// </summary>
    Task<ProjectShopOwnerResponse> CompleteAsync(long accountId, long id);

    /// <summary>
    /// [OWNER — HUỶ DỰ ÁN] briefed/in_progress → cancelled. Engagement đang mở bị đóng theo
    /// (requested → rejected, accepted → terminated), post 'open' → 'closed'.
    /// </summary>
    Task<ProjectShopOwnerResponse> CancelAsync(long accountId, long id);

    Task DeleteAsync(long accountId, long id);
}
