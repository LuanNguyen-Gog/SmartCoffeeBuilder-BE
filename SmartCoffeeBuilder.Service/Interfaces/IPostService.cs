using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Post;
using SmartCoffeeBuilder.Service.DTOs.Responses.Post;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IPostService
{
    /// <summary>
    /// Tìm/duyệt bài đăng. Provider thường lọc status=open, serviceKind theo capability.
    /// </summary>
    Task<PaginationResponse<PostResponse>> GetAllAsync(
        int pageNumber = 1,
        int pageSize = 10,
        long? projectShopOwnerId = null,
        string? serviceKind = null,
        string? status = null,
        string? search = null);

    Task<PostResponse> GetByIdAsync(long id);
    Task<PostResponse> CreateAsync(CreatePostRequest request);
    Task<PostResponse> UpdateAsync(long id, UpdatePostRequest request);
    Task DeleteAsync(long id);
}
