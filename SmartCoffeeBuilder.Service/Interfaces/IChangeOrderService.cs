using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ChangeOrder;
using SmartCoffeeBuilder.Service.DTOs.Responses.ChangeOrder;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Phát sinh chi phí ngoài báo giá đã chốt (review 1.1: phí sửa, đổi phạm vi, đổi vật tư).
/// </summary>
public interface IChangeOrderService
{
    Task<PaginationResponse<ChangeOrderResponse>> GetAllAsync(
        Guid accountId, Guid projectWorkingId, string? status = null,
        int pageNumber = 1, int pageSize = 20);

    Task<ChangeOrderResponse> GetByIdAsync(Guid accountId, Guid id);
    Task<ChangeOrderResponse> CreateAsync(Guid accountId, CreateChangeOrderRequest request);
    Task<ChangeOrderResponse> UpdateAsync(Guid accountId, Guid id, UpdateChangeOrderRequest request);

    /// <summary>Bên KIA đồng ý — khoản phát sinh được khoá và cộng vào công nợ.</summary>
    Task<ChangeOrderResponse> AcceptAsync(Guid accountId, Guid id);

    /// <summary>Bên KIA từ chối, kèm lý do.</summary>
    Task<ChangeOrderResponse> RejectAsync(Guid accountId, Guid id, RejectChangeOrderRequest request);

    /// <summary>Bên đã lập rút lại khoản còn 'pending'.</summary>
    Task DeleteAsync(Guid accountId, Guid id);

    /// <summary>Tổng công nợ: giá trị hợp đồng + phát sinh đã duyệt.</summary>
    Task<ChangeOrderSummaryResponse> GetSummaryAsync(Guid accountId, Guid projectWorkingId);

    /// <summary>Hạn mức sửa còn lại của một bản thiết kế.</summary>
    Task<RevisionQuotaResponse> GetRevisionQuotaAsync(Guid accountId, Guid designId);
}
