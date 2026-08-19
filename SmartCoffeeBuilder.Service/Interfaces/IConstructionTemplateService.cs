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
}
