using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Checklist;
using SmartCoffeeBuilder.Service.DTOs.Responses.Checklist;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Checklist nghiệm thu cho bản thiết kế / hạng mục thi công (review 3).
/// Provider lập mục, OWNER chấm đạt–chưa đạt kèm minh chứng và ghi chú cần sửa gì.
/// </summary>
public interface IChecklistItemService
{
    Task<PaginationResponse<ChecklistItemResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 50,
        Guid? designId = null, Guid? constructionItemId = null, string? status = null);

    Task<ChecklistItemResponse> GetByIdAsync(Guid accountId, Guid id);

    Task<List<ChecklistItemResponse>> CreateAsync(Guid accountId, CreateChecklistItemsRequest request);

    Task<ChecklistItemResponse> UpdateAsync(Guid accountId, Guid id, UpdateChecklistItemRequest request);

    /// <summary>Owner chấm đạt / chưa đạt (chấm lại được nhiều lần).</summary>
    Task<ChecklistItemResponse> CheckAsync(Guid accountId, Guid id, CheckChecklistItemRequest request);

    /// <summary>Provider đính minh chứng cho một mục.</summary>
    Task<ChecklistItemResponse> AttachEvidenceAsync(Guid accountId, Guid id, AttachChecklistEvidenceRequest request);

    Task DeleteAsync(Guid accountId, Guid id);
}
