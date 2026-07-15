using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Issue;
using SmartCoffeeBuilder.Service.DTOs.Responses.Issue;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IIssueService
{
    Task<PaginationResponse<IssueResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectWorkingId = null, long? constructionItemId = null, string? status = null);

    Task<IssueResponse> GetByIdAsync(long id);

    /// <summary>Tạo issue neo vào engagement; nếu gắn construction_item thì phải cùng engagement.</summary>
    Task<IssueResponse> CreateAsync(CreateIssueRequest request);

    Task<IssueResponse> UpdateAsync(long id, UpdateIssueRequest request);

    /// <summary>Chuyển trạng thái: open → in_progress → resolved → closed (chỉ tiến, không lùi).</summary>
    Task<IssueResponse> UpdateStatusAsync(long id, UpdateIssueStatusRequest request);

    Task DeleteAsync(long id);
}
