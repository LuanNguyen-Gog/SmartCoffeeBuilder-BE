using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.Messaging;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAiRecommendationService
{
    // GET all by briefId (paginated)
    Task<PaginationResponse<AiRecommendationResponse>> GetAllByBriefIdAsync(
        long briefId, int pageNumber = 1, int pageSize = 10);
    Task<AiRecommendationResponse> GetByIdAsync(long id);

    // POST - generate AI design
    Task<AiDesignJobStatusResponse> GenerateDesignAsync(long briefId, string userId, GenerateAiDesignRequest request);

    // CRUD (kept for manual management)
    Task<AiRecommendationResponse> CreateAsync(CreateAiRecommendationRequest request);
    Task<AiRecommendationResponse> UpdateAsync(long id, UpdateAiRecommendationRequest request);
    Task DeleteAsync(long id);
    
    // Process result from Pub/Sub
    Task ProcessAiDesignResultAsync(AiDesignResultMessage result);
}
