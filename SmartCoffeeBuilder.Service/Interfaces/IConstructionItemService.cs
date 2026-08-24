using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Mọi method nhận <c>accountId</c> lấy từ JWT ở controller: quyền xét theo ENGAGEMENT chứa
/// milestone, không xét theo AccountRole (hai provider khác nhau trên cùng dự án có role giống hệt).
/// </summary>
public interface IConstructionItemService
{
    /// <summary>Danh sách milestone — đã lọc theo engagement mà tài khoản tham gia (admin xem tất cả).</summary>
    Task<PaginationResponse<ConstructionItemResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? parentId = null, string? status = null);

    Task<ConstructionItemResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>
    /// Constructor tạo milestone. Guard: là provider của engagement, engagement 'accepted',
    /// contract 'confirmed', contract_type có construction; parent (nếu có) phải cùng engagement.
    /// </summary>
    Task<ConstructionItemResponse> CreateAsync(Guid accountId, CreateConstructionItemRequest request);

    Task<ConstructionItemResponse> UpdateAsync(Guid accountId, Guid id, UpdateConstructionItemRequest request);

    /// <summary>Chuyển trạng thái: pending → in_progress → completed (chỉ tiến, không lùi, không cancel).</summary>
    Task<ConstructionItemResponse> UpdateStatusAsync(
        Guid accountId, Guid id, UpdateConstructionItemStatusRequest request);

    Task DeleteAsync(Guid accountId, Guid id);

    /// <summary>
    /// Chi phí của một hạng mục: nhân công (hạng mục + task con) CỘNG vật tư, gộp cả milestone con.
    /// </summary>
    Task<ConstructionCostSummaryResponse> GetCostSummaryAsync(Guid accountId, Guid id);

    /// <summary>Chi phí thi công của cả hợp tác — cộng từ mọi milestone gốc.</summary>
    Task<EngagementCostSummaryResponse> GetEngagementCostSummaryAsync(Guid accountId, Guid projectWorkingId);

    /// <summary>
    /// JOB NỀN (Hangfire, chạy hằng ngày) — KHÔNG phải endpoint, không nhận accountId: quét mọi
    /// hạng mục quá hạn <c>estimate_at</c> mà chưa xong trên các engagement còn hoạt động, rồi
    /// báo cho chủ quán tương ứng. Chỉ CẢNH BÁO: nền tảng không giữ tiền và không tự khấu trừ,
    /// owner tự làm việc với nhà cung cấp.
    /// </summary>
    /// <param name="renotifyAfterDays">Số ngày tối thiểu giữa hai lần báo cho cùng một hạng mục.</param>
    /// <returns>Số noti thực sự được tạo trong lượt quét.</returns>
    Task<int> NotifyOverdueProgressAsync(int renotifyAfterDays = 7);
}
