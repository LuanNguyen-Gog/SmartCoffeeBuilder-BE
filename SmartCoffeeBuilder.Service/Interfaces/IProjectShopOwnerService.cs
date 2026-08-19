using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectShopOwner;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IProjectShopOwnerService
{
    /// <summary>
    /// Chỉ trả dự án người gọi tham gia (chủ dự án / provider có engagement) hoặc đang mở thầu
    /// công khai; admin thấy tất cả. Lọc trong query để phân trang đúng.
    /// </summary>
    Task<PaginationResponse<ProjectShopOwnerResponse>> GetAllAsync(Guid accountId, int pageNumber = 1, int pageSize = 10, Guid? ownerId = null);
    Task<ProjectShopOwnerResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>
    /// <c>request.OwnerId</c> phải là hồ sơ chủ quán của chính <paramref name="accountId"/>
    /// (admin được tạo hộ). Giữ field trong body để không phá hợp đồng API sẵn có.
    /// </summary>
    Task<ProjectShopOwnerResponse> CreateAsync(Guid accountId, CreateProjectShopOwnerRequest request);

    /// <summary>
    /// Sửa thông tin dự án. Status chỉ nhận transition thường (briefed → in_progress);
    /// đóng dự án (completed/cancelled) phải qua <see cref="CompleteAsync"/> / <see cref="CancelAsync"/>.
    /// </summary>
    Task<ProjectShopOwnerResponse> UpdateAsync(Guid accountId, Guid id, UpdateProjectShopOwnerRequest request);

    /// <summary>
    /// [OWNER — ĐÓNG DỰ ÁN] in_progress → completed. Yêu cầu mọi engagement đã đóng
    /// và có ít nhất một engagement được nghiệm thu. Post còn 'open' bị đóng theo.
    /// </summary>
    Task<ProjectShopOwnerResponse> CompleteAsync(Guid accountId, Guid id);

    /// <summary>
    /// [OWNER — HUỶ DỰ ÁN] briefed/in_progress → cancelled. Engagement đang mở bị đóng theo
    /// (requested → rejected, accepted → terminated), post 'open' → 'closed'.
    /// </summary>
    Task<ProjectShopOwnerResponse> CancelAsync(Guid accountId, Guid id);

    Task DeleteAsync(Guid accountId, Guid id);
}
