using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Apply;
using SmartCoffeeBuilder.Service.DTOs.Responses.Apply;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Hồ sơ ứng tuyển có ĐÚNG HAI bên: provider nộp hồ sơ, và chủ của bài đăng. Mọi method nhận
/// <c>accountId</c> (controller lấy từ <c>User.GetAccountId()</c>) vì role gate không phân biệt
/// được owner A với owner B, hay provider A với provider B.
/// </summary>
public interface IApplyService
{
    /// <summary>
    /// Danh sách hồ sơ ứng tuyển. Chỉ trả về hồ sơ mà account này là một bên — provider thấy hồ sơ
    /// mình nộp, owner thấy hồ sơ nộp vào bài của mình; admin thấy tất cả.
    /// </summary>
    /// <remarks>
    /// Lọc TRONG query chứ không lọc sau: <c>Proposal</c> và <c>EstimatedDurationDays</c> là nội
    /// dung chào thầu, để hở thì provider chỉ cần đổi <c>postId</c> là đọc được bài của đối thủ.
    /// </remarks>
    Task<PaginationResponse<ApplyResponse>> GetAllAsync(
        Guid accountId,
        int pageNumber = 1, int pageSize = 10,
        Guid? postId = null, Guid? serviceProviderProfileId = null, string? status = null);

    /// <summary>Chi tiết một hồ sơ — chỉ hai bên của chính hồ sơ đó (hoặc admin).</summary>
    Task<ApplyResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>Provider nộp hồ sơ ứng tuyển vào bài đăng đang mở — hồ sơ provider tra từ accountId của người đăng nhập.</summary>
    Task<ApplyResponse> ApplyAsync(Guid accountId, CreateApplyRequest request);

    /// <summary>Provider sửa proposal/thời gian dự kiến khi hồ sơ còn pending — chỉ provider ĐÃ NỘP hồ sơ đó.</summary>
    Task<ApplyResponse> UpdateProposalAsync(Guid accountId, Guid id, UpdateApplyRequest request);

    /// <summary>
    /// Owner chấp nhận hồ sơ: application→accepted, tạo project_provider (đường marketplace),
    /// đóng post, từ chối các hồ sơ pending còn lại. Chỉ chủ CỦA BÀI ĐĂNG — đây là quyết định
    /// chọn nhà thầu, để hở thì owner khác chốt hộ được.
    /// </summary>
    Task<ProjectWorkingResponse> AcceptAsync(Guid accountId, Guid id);

    /// <summary>Owner từ chối hồ sơ pending — chỉ chủ của bài đăng.</summary>
    Task<ApplyResponse> RejectAsync(Guid accountId, Guid id);

    /// <summary>Provider rút hồ sơ khi còn pending (xoá hẳn bản ghi) — chỉ provider đã nộp hồ sơ đó.</summary>
    Task WithdrawAsync(Guid accountId, Guid id);
}
