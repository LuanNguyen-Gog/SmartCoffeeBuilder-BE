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
}
