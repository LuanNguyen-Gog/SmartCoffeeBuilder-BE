using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Issue;
using SmartCoffeeBuilder.Service.DTOs.Responses.Issue;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Issue neo vào engagement. Mọi method nhận <c>accountId</c> (controller lấy từ
/// <c>User.GetAccountId()</c>) để tự resolve quyền — hợp đồng HTTP không đổi.
/// </summary>
public interface IIssueService
{
    /// <summary>
    /// Danh sách issue. Chỉ trả về issue của những engagement mà account này là một bên;
    /// admin thấy tất cả.
    /// </summary>
    Task<PaginationResponse<IssueResponse>> GetAllAsync(
        Guid accountId,
        int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? constructionItemId = null, string? status = null);

    Task<IssueResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>
    /// Tạo issue neo vào engagement; nếu gắn construction_item thì phải cùng engagement.
    /// Người tạo phải là một bên của engagement đó.
    /// </summary>
    Task<IssueResponse> CreateAsync(Guid accountId, CreateIssueRequest request);

    Task<IssueResponse> UpdateAsync(Guid accountId, Guid id, UpdateIssueRequest request);

    /// <summary>Chuyển trạng thái: open → in_progress → resolved → closed (chỉ tiến, không lùi).</summary>
    Task<IssueResponse> UpdateStatusAsync(Guid accountId, Guid id, UpdateIssueStatusRequest request);

    /// <summary>Chỉ admin (rào ở role gate của controller).</summary>
    Task DeleteAsync(Guid id);
}
