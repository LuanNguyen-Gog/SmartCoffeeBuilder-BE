using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Survey;
using SmartCoffeeBuilder.Service.DTOs.Responses.Survey;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface ISurveyService
{
    Task<PaginationResponse<SurveyResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10, long? projectWorkingId = null);
    Task<SurveyResponse> GetByIdAsync(long id);
    Task<SurveyResponse> CreateAsync(long accountId, CreateSurveyRequest request);
    Task<SurveyResponse> UpdateAsync(long accountId, long id, UpdateSurveyRequest request);
}
