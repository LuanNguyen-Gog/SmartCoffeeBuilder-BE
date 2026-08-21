using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Survey;
using SmartCoffeeBuilder.Service.DTOs.Responses.Survey;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface ISurveyService
{
    /// <summary>
    /// Khảo sát trong tầm nhìn của người gọi (owner của dự án / provider đứng tên / admin).
    /// <paramref name="postId"/> để owner so khảo sát của mọi provider trên một bài đăng.
    /// </summary>
    Task<PaginationResponse<SurveyResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? applyId = null, Guid? postId = null);

    Task<SurveyResponse> GetByIdAsync(Guid accountId, Guid id);
    Task<SurveyResponse> CreateAsync(Guid accountId, CreateSurveyRequest request);
    Task<SurveyResponse> UpdateAsync(Guid accountId, Guid id, UpdateSurveyRequest request);
}
