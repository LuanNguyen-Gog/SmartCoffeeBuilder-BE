using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Issue;
using SmartCoffeeBuilder.Service.DTOs.Responses.Issue;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IIssueService
{
    Task<PaginationResponse<IssueResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? constructionItemId = null, string? status = null);

    Task<IssueResponse> GetByIdAsync(Guid id);

    /// <summary>Tạo issue neo vào engagement; nếu gắn construction_item thì phải cùng engagement.</summary>
    Task<IssueResponse> CreateAsync(CreateIssueRequest request);

    Task<IssueResponse> UpdateAsync(Guid id, UpdateIssueRequest request);

    /// <summary>Chuyển trạng thái: open → in_progress → resolved → closed (chỉ tiến, không lùi).</summary>
    Task<IssueResponse> UpdateStatusAsync(Guid id, UpdateIssueStatusRequest request);

    Task DeleteAsync(Guid id);
}
