using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Post;
using SmartCoffeeBuilder.Service.DTOs.Responses.Post;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IPostService
{
    /// <summary>
    /// Tìm/duyệt bài đăng.
    /// </summary>
    /// <param name="accountId">
    /// Người đang đăng nhập. Nếu là provider đã có hồ sơ, danh sách tự lọc theo
    /// capability của họ (xem <c>ProviderCapability</c>) — designer không thấy
    /// bài thi công và ngược lại. Owner/admin không bị lọc.
    /// </param>
    Task<PaginationResponse<PostResponse>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        Guid? projectShopOwnerId = null,
        string? serviceKind = null,
        string? status = null,
        string? search = null,
        Guid? accountId = null);

    Task<PostResponse> GetByIdAsync(Guid id);
    Task<PostResponse> CreateAsync(CreatePostRequest request);
    Task<PostResponse> UpdateAsync(Guid id, UpdatePostRequest request);
    Task DeleteAsync(Guid id);

    /// <summary>
    /// [HANGFIRE JOB] Bài 'open' đã qua submission_deadline → 'closed', và mọi hồ sơ còn 'pending'
    /// của chúng → 'rejected' kèm noti cho provider. Không có job này thì bài quá hạn chỉ bị ẩn
    /// khỏi danh sách còn hồ sơ treo mãi ở 'pending'.
    /// </summary>
    Task CloseExpiredPostsAsync();
}
