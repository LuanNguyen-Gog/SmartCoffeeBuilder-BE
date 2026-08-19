using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Material;
using SmartCoffeeBuilder.Service.DTOs.Responses.Material;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IMaterialService
{
    // ── Bảng giá vật tư của engagement ─────────────────────────────────────
    Task<PaginationResponse<MaterialResponse>> GetAllAsync(
        Guid accountId, Guid projectWorkingId, int pageNumber = 1, int pageSize = 50);
    Task<MaterialResponse> GetByIdAsync(Guid accountId, Guid id);
    Task<MaterialResponse> CreateAsync(Guid accountId, CreateMaterialRequest request);
    Task<MaterialResponse> UpdateAsync(Guid accountId, Guid id, UpdateMaterialRequest request);
    Task DeleteAsync(Guid accountId, Guid id);

    // ── Lượng vật tư dùng theo hạng mục / task ─────────────────────────────
    Task<List<ConstructionMaterialResponse>> GetUsagesAsync(
        Guid accountId, Guid? constructionItemId, Guid? constructionTaskId);
    Task<ConstructionMaterialResponse> AddUsageAsync(
        Guid accountId, CreateConstructionMaterialRequest request);
    Task<ConstructionMaterialResponse> UpdateUsageAsync(
        Guid accountId, Guid id, UpdateConstructionMaterialRequest request);
    Task RemoveUsageAsync(Guid accountId, Guid id);

    /// <summary>Chi phí vật tư của một milestone = phần riêng + gộp từ mọi task con.</summary>
    Task<MaterialCostSummaryResponse> GetItemCostAsync(Guid accountId, Guid constructionItemId);
}
