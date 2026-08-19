using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Apply;
using SmartCoffeeBuilder.Service.DTOs.Responses.Apply;
using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IApplyService
{
    Task<PaginationResponse<ApplyResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        Guid? postId = null, Guid? serviceProviderProfileId = null, string? status = null);

    Task<ApplyResponse> GetByIdAsync(Guid id);

    /// <summary>Provider nộp hồ sơ ứng tuyển vào bài đăng đang mở — hồ sơ provider tra từ accountId của người đăng nhập.</summary>
    Task<ApplyResponse> ApplyAsync(Guid accountId, CreateApplyRequest request);

    /// <summary>Provider sửa proposal/thời gian dự kiến khi hồ sơ còn pending.</summary>
    Task<ApplyResponse> UpdateProposalAsync(Guid id, UpdateApplyRequest request);

    /// <summary>
    /// Owner chấp nhận hồ sơ: application→accepted, tạo project_provider (đường marketplace),
    /// đóng post, từ chối các hồ sơ pending còn lại.
    /// </summary>
    Task<ProjectWorkingResponse> AcceptAsync(Guid id);

    /// <summary>Owner từ chối hồ sơ pending.</summary>
    Task<ApplyResponse> RejectAsync(Guid id);

    /// <summary>Provider rút hồ sơ khi còn pending — xoá hẳn bản ghi.</summary>
    Task WithdrawAsync(Guid id);
}
