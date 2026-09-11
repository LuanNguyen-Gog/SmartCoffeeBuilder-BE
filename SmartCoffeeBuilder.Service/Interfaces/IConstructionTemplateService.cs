using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTemplate;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTemplate;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Mẫu quy trình thi công tái dùng (review 3). Áp mẫu là COPY một lần sang construction_item /
/// construction_task — sửa mẫu về sau không đụng dự án đã áp.
/// </summary>
public interface IConstructionTemplateService
{
    Task<PaginationResponse<ConstructionTemplateResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10, string? serviceKind = null);

    Task<ConstructionTemplateResponse> GetByIdAsync(Guid accountId, Guid id);

    Task<ConstructionTemplateResponse> CreateAsync(Guid accountId, CreateConstructionTemplateRequest request);

    Task DeleteAsync(Guid accountId, Guid id);

    /// <summary>Sinh hạng mục + việc con cho một engagement đã ký hợp đồng.</summary>
    Task<ApplyTemplateResponse> ApplyAsync(Guid accountId, Guid id, ApplyConstructionTemplateRequest request);

    /// <summary>
    /// Các mẫu quy trình đã được áp vào một engagement — đọc ngược từ vết nguồn trên
    /// construction_items. CẢ HAI BÊN của engagement đều xem được: review 3 yêu cầu chủ quán
    /// nhìn thấy nhà thầu đang chạy theo quy trình nào.
    /// </summary>
    Task<List<AppliedConstructionTemplateResponse>> GetAppliedAsync(Guid accountId, Guid projectWorkingId);

    /// <summary>
    /// Sắp lại thứ tự hạng mục trong mẫu — chỉ tác giả mẫu (hoặc admin). Nhận toàn bộ danh sách.
    /// </summary>
    Task<ConstructionTemplateResponse> ReorderItemsAsync(
        Guid accountId, Guid id, ReorderConstructionTemplateItemsRequest request);
}
