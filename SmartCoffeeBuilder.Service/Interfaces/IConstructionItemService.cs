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
}
