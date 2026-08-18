using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.Messaging;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAiRecommendationService
{
    // GET (paginated) — accountId lấy từ JWT. Quyền ĐỌC rộng hơn quyền ghi: chủ dự án, provider
    // có engagement còn hiệu lực, mọi tài khoản khi dự án còn bài đăng 'open', và admin.
    // Xem EnsureBriefVisibleAsync; luật khớp DesignBriefService.EnsureProjectVisibleAsync.
    Task<PaginationResponse<AiRecommendationResponse>> GetAllByBriefIdAsync(
        Guid accountId, Guid briefId, int pageNumber = 1, int pageSize = 10);
    Task<AiRecommendationResponse> GetByIdAsync(Guid accountId, Guid id);

    // POST - generate AI design (userId là account id dạng chuỗi, đọc từ claim sub).
    // Đường GHI: chỉ chủ dự án hoặc admin (EnsureBriefOwnerAsync) — job tốn quota.
    Task<AiDesignJobStatusResponse> GenerateDesignAsync(Guid briefId, string userId, GenerateAiDesignRequest request);

    // CRUD nội bộ — KHÔNG có route HTTP nào trỏ tới (chỉ dùng cho quản lý thủ công / consumer),
    // nên không rào quyền ở đây. Nếu sau này expose ra controller thì phải thêm EnsureBriefOwnerAsync.
    Task<AiRecommendationResponse> CreateAsync(CreateAiRecommendationRequest request);
    Task<AiRecommendationResponse> UpdateAsync(Guid id, UpdateAiRecommendationRequest request);
    Task DeleteAsync(Guid id);
    
    // Process result from Pub/Sub
    Task ProcessAiDesignResultAsync(AiDesignResultMessage result);
}
