using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Mọi method nhận <c>accountId</c> lấy từ JWT ở controller: quyền xét theo ENGAGEMENT chứa design
/// (provider soạn/nộp/sửa, owner duyệt/yêu cầu sửa), không xét theo AccountRole.
/// </summary>
public interface IDesignService
{
    /// <summary>Danh sách design — đã lọc theo engagement mà tài khoản tham gia (admin xem tất cả).</summary>
    Task<PaginationResponse<DesignResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, string? status = null, string? type = null);
    Task<DesignResponse> GetByIdAsync(Guid accountId, Guid id);
    Task<DesignResponse> CreateAsync(Guid accountId, CreateDesignRequest request);
    Task<DesignResponse> UpdateAsync(Guid accountId, Guid id, UpdateDesignRequest request);

    // Vòng duyệt/revision: in_progress → submitted → approved | revision → in_progress → submitted…
    // accountId lấy từ JWT ở controller — vừa để check quyền, vừa ghi vào design_versions.snapshotted_by.
    Task<DesignResponse> SubmitAsync(Guid id, Guid accountId);
    Task<DesignResponse> ApproveAsync(Guid id, Guid accountId);
    Task<DesignResponse> RequestRevisionAsync(Guid accountId, Guid id, RequestDesignRevisionRequest request);
    Task<DesignResponse> StartRevisionAsync(Guid accountId, Guid id);

    // File/ảnh của design — upload thẳng lên GCS (folder "designs"), DB lưu objectName.
    Task<DesignImageResponse> UploadFileAsync(
        Guid accountId, Guid designId, Stream content, string fileName, string? contentType, long sizeBytes,
        string? caption = null, Guid? uploadedBy = null);
    Task RemoveFileAsync(Guid accountId, Guid designId, Guid imageId);

    // Lịch sử version (snapshot khi submit / approve) — dùng để truy nguyên sau revision.
    // Mỗi submit / approve đều sinh 1 bản mới (full history), không upsert — có thể trả về nhiều bản.
    Task<PaginationResponse<DesignVersionResponse>> GetVersionsAsync(
        Guid accountId, Guid designId, int pageNumber = 1, int pageSize = 20);
    Task<DesignVersionResponse> GetVersionByIdAsync(Guid accountId, Guid designId, Guid versionId);
}
