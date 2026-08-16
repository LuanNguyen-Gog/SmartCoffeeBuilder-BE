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
    /// <summary>
    /// Danh sách task — đã lọc theo engagement mà tài khoản tham gia (admin xem tất cả).
    /// <paramref name="projectWorkingId"/> thu hẹp về ĐÚNG MỘT engagement: không có nó thì client
    /// muốn đếm task của một dự án buộc phải lấy hết task của mọi engagement rồi tự lọc, và con số
    /// tổng sẽ dính task của dự án khác.
    /// </summary>
    Task<PaginationResponse<ConstructionTaskResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? constructionItemId = null, string? status = null, Guid? projectWorkingId = null);

    Task<ConstructionTaskResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>Tạo task trong milestone. Phải là provider của engagement; milestone chưa 'completed'.</summary>
    Task<ConstructionTaskResponse> CreateAsync(Guid accountId, CreateConstructionTaskRequest request);

    Task<ConstructionTaskResponse> UpdateAsync(Guid accountId, Guid id, UpdateConstructionTaskRequest request);

    /// <summary>Chuyển trạng thái: pending → in_progress → completed (chỉ tiến, không lùi).</summary>
    Task<ConstructionTaskResponse> UpdateStatusAsync(
        Guid accountId, Guid id, UpdateConstructionTaskStatusRequest request);

    Task DeleteAsync(Guid accountId, Guid id);
}
