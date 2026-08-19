using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Survey;
using SmartCoffeeBuilder.Service.DTOs.Responses.Survey;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface ISurveyService
{
    Task<PaginationResponse<SurveyResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10, Guid? projectWorkingId = null, Guid? applyId = null);
    Task<SurveyResponse> GetByIdAsync(Guid id);
    Task<SurveyResponse> CreateAsync(Guid accountId, CreateSurveyRequest request);
    Task<SurveyResponse> UpdateAsync(Guid accountId, Guid id, UpdateSurveyRequest request);
}
