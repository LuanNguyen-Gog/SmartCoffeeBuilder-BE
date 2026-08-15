using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.Messaging;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAiRecommendationService
{
    // GET all by briefId (paginated) — accountId lấy từ JWT, brief phải thuộc dự án của người gọi.
    Task<PaginationResponse<AiRecommendationResponse>> GetAllByBriefIdAsync(
        long accountId, long briefId, int pageNumber = 1, int pageSize = 10);
    Task<AiRecommendationResponse> GetByIdAsync(long accountId, long id);

    // POST - generate AI design (userId là account id dạng chuỗi, đọc từ claim sub)
    Task<AiDesignJobStatusResponse> GenerateDesignAsync(long briefId, string userId, GenerateAiDesignRequest request);

    // CRUD nội bộ — KHÔNG có route HTTP nào trỏ tới (chỉ dùng cho quản lý thủ công / consumer),
    // nên không rào quyền ở đây. Nếu sau này expose ra controller thì phải thêm EnsureBriefOwnerAsync.
    Task<AiRecommendationResponse> CreateAsync(CreateAiRecommendationRequest request);
    Task<AiRecommendationResponse> UpdateAsync(long id, UpdateAiRecommendationRequest request);
    Task DeleteAsync(long id);
    
    // Process result from Pub/Sub
    Task ProcessAiDesignResultAsync(AiDesignResultMessage result);
}
