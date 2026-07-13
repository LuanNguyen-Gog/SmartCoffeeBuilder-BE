using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IConstructionItemService
{
    Task<PaginationResponse<ConstructionItemResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectProviderId = null, long? parentId = null, string? status = null);

    Task<ConstructionItemResponse> GetByIdAsync(long id);

    /// <summary>
    /// Constructor tạo milestone. Guard: engagement 'accepted', contract 'confirmed',
    /// contract_type có construction; parent (nếu có) phải cùng engagement.
    /// </summary>
    Task<ConstructionItemResponse> CreateAsync(CreateConstructionItemRequest request);

    Task<ConstructionItemResponse> UpdateAsync(long id, UpdateConstructionItemRequest request);

    /// <summary>Chuyển trạng thái: pending → in_progress → completed (chỉ tiến, không lùi, không cancel).</summary>
    Task<ConstructionItemResponse> UpdateStatusAsync(long id, UpdateConstructionItemStatusRequest request);

    Task DeleteAsync(long id);
}
