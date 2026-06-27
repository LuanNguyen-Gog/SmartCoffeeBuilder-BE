using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAiRecommendationService
{
    Task<PaginationResponse<AiRecommendationResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10, long? briefId = null);
    Task<AiRecommendationResponse> GetByIdAsync(long id);
    Task<AiRecommendationResponse> CreateAsync(CreateAiRecommendationRequest request);
    Task<AiRecommendationResponse> UpdateAsync(long id, UpdateAiRecommendationRequest request);
    Task DeleteAsync(long id);
}
