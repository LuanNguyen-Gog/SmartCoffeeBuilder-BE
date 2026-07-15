using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Review;
using SmartCoffeeBuilder.Service.DTOs.Responses.Review;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IReviewService
{
    Task<PaginationResponse<ReviewResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectWorkingId = null, long? serviceProviderProfileId = null);

    Task<ReviewResponse> GetByIdAsync(long id);

    Task<ProviderRatingSummaryResponse> GetProviderSummaryAsync(long serviceProviderProfileId);

    Task<ReviewResponse> CreateAsync(CreateReviewRequest request);

    Task<ReviewResponse> UpdateAsync(long id, UpdateReviewRequest request);

    Task DeleteAsync(long id);
}
