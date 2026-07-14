using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Review;
using SmartCoffeeBuilder.Service.DTOs.Responses.Review;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IReviewService
{
    Task<PaginationResponse<ReviewResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectProviderId = null, long? providerId = null);

    Task<ReviewResponse> GetByIdAsync(long id);

    Task<ProviderRatingSummaryResponse> GetProviderSummaryAsync(long providerId);

    Task<ReviewResponse> CreateAsync(CreateReviewRequest request);

    Task<ReviewResponse> UpdateAsync(long id, UpdateReviewRequest request);

    Task DeleteAsync(long id);
}
