using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTask;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IConstructionTaskService
{
    Task<PaginationResponse<ConstructionTaskResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? constructionItemId = null, string? status = null);

    Task<ConstructionTaskResponse> GetByIdAsync(long id);

    /// <summary>Tạo task trong milestone. Milestone phải chưa 'completed'.</summary>
    Task<ConstructionTaskResponse> CreateAsync(CreateConstructionTaskRequest request);

    Task<ConstructionTaskResponse> UpdateAsync(long id, UpdateConstructionTaskRequest request);

    /// <summary>Chuyển trạng thái: pending → in_progress → completed (chỉ tiến, không lùi).</summary>
    Task<ConstructionTaskResponse> UpdateStatusAsync(long id, UpdateConstructionTaskStatusRequest request);

    Task DeleteAsync(long id);
}
