using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.DesignBrief;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Brief là tài liệu của MỘT dự án. Mọi method nhận <c>accountId</c> lấy từ JWT ở controller
/// (<c>User.GetAccountId()</c>) để tự resolve quyền — hợp đồng HTTP không đổi.
/// </summary>
public interface IDesignBriefService
{
    /// <summary>Chỉ trả brief của dự án người gọi tham gia (hoặc dự án đang mở thầu). Lọc trong query.</summary>
    Task<PaginationResponse<DesignBriefResponse>> GetAllAsync(long accountId, int pageNumber = 1, int pageSize = 10, long? projectShopOwnerId = null);
    Task<DesignBriefResponse> GetByIdAsync(long accountId, long id);
    Task<DesignBriefResponse> CreateAsync(long accountId, CreateDesignBriefRequest request);
    Task<DesignBriefResponse> UpdateAsync(long accountId, long id, UpdateDesignBriefRequest request);
    Task DeleteAsync(long accountId, long id);
}
