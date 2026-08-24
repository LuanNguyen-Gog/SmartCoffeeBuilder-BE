using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.DailyLog;
using SmartCoffeeBuilder.Service.DTOs.Responses.DailyLog;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Nhật ký thi công hằng ngày (review 3). Nhà cung cấp ghi, chủ quán đọc — đây là kênh để owner
/// theo dõi công trường mà không phải đến tận nơi.
///
/// Quyền đi theo engagement (<c>EngagementAuthorization</c>): GHI chỉ nhà cung cấp của engagement,
/// ĐỌC cả hai bên. Không mở công khai — nhật ký chứa ảnh hiện trường của dự án riêng.
/// </summary>
public interface IDailyLogService
{
    /// <summary>
    /// Nhật ký theo engagement / hạng mục / task, mới nhất trước. Chỉ trả về những engagement mà
    /// tài khoản là một bên (admin xem tất cả) — lọc TRONG query để phân trang không sai tổng.
    /// </summary>
    Task<PaginationResponse<DailyLogResponse>> GetAllAsync(
        Guid accountId,
        int pageNumber = 1, int pageSize = 20,
        Guid? projectWorkingId = null,
        Guid? constructionItemId = null,
        Guid? constructionTaskId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null);

    Task<DailyLogResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>Nhà cung cấp ghi nhật ký cho một ngày.</summary>
    Task<DailyLogResponse> CreateAsync(Guid accountId, CreateDailyLogRequest request);

    /// <summary>Sửa nhật ký — chỉ nhà cung cấp của engagement.</summary>
    Task<DailyLogResponse> UpdateAsync(Guid accountId, Guid id, UpdateDailyLogRequest request);

    /// <summary>Xoá nhật ký (kèm file đính kèm trên bucket).</summary>
    Task DeleteAsync(Guid accountId, Guid id);
}
