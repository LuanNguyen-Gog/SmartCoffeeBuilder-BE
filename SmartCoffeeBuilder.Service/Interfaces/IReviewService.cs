using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Review;
using SmartCoffeeBuilder.Service.DTOs.Responses.Review;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IReviewService
{
    Task<PaginationResponse<ReviewResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? serviceProviderProfileId = null);

    Task<ReviewResponse> GetByIdAsync(Guid id);

    Task<ProviderRatingSummaryResponse> GetProviderSummaryAsync(Guid serviceProviderProfileId);

    Task<ReviewResponse> CreateAsync(Guid accountId, CreateReviewRequest request);

    Task<ReviewResponse> UpdateAsync(Guid accountId, Guid id, UpdateReviewRequest request);

    Task DeleteAsync(Guid accountId, Guid id);

    /// <summary>Nhà cung cấp trả lời công khai một đánh giá — ghi đè phản hồi cũ (review 1.1).</summary>
    Task<ReviewResponse> ReplyAsync(Guid accountId, Guid id, ReplyReviewRequest request);

    /// <summary>Nhà cung cấp gỡ phản hồi của mình.</summary>
    Task<ReviewResponse> RemoveReplyAsync(Guid accountId, Guid id);

    /// <summary>Chủ quán đính ảnh thành phẩm vào đánh giá của mình.</summary>
    Task<ReviewImageResponse> AddImageAsync(Guid accountId, Guid id, ReviewImageRequest request);

    Task RemoveImageAsync(Guid accountId, Guid imageId);
}
