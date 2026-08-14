using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTask;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Mọi method nhận <c>accountId</c> lấy từ JWT ở controller: quyền xét theo ENGAGEMENT của
/// milestone cha, không xét theo AccountRole.
/// </summary>
public interface IConstructionTaskService
{
    /// <summary>Danh sách task — đã lọc theo engagement mà tài khoản tham gia (admin xem tất cả).</summary>
    Task<PaginationResponse<ConstructionTaskResponse>> GetAllAsync(
        long accountId, int pageNumber = 1, int pageSize = 10,
        long? constructionItemId = null, string? status = null);

    Task<ConstructionTaskResponse> GetByIdAsync(long accountId, long id);

    /// <summary>Tạo task trong milestone. Phải là provider của engagement; milestone chưa 'completed'.</summary>
    Task<ConstructionTaskResponse> CreateAsync(long accountId, CreateConstructionTaskRequest request);

    Task<ConstructionTaskResponse> UpdateAsync(long accountId, long id, UpdateConstructionTaskRequest request);

    /// <summary>Chuyển trạng thái: pending → in_progress → completed (chỉ tiến, không lùi).</summary>
    Task<ConstructionTaskResponse> UpdateStatusAsync(
        long accountId, long id, UpdateConstructionTaskStatusRequest request);

    Task DeleteAsync(long accountId, long id);
}
