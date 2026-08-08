using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IDesignService
{
    Task<PaginationResponse<DesignResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? projectWorkingId = null, string? status = null, string? type = null);
    Task<DesignResponse> GetByIdAsync(long id);
    Task<DesignResponse> CreateAsync(CreateDesignRequest request);
    Task<DesignResponse> UpdateAsync(long id, UpdateDesignRequest request);

    // Vòng duyệt/revision: in_progress → submitted → approved | revision → in_progress → submitted…
    Task<DesignResponse> SubmitAsync(long id);
    Task<DesignResponse> ApproveAsync(long id);
    Task<DesignResponse> RequestRevisionAsync(long id, RequestDesignRevisionRequest request);
    Task<DesignResponse> StartRevisionAsync(long id);

    // File/ảnh của design — upload thẳng lên GCS (folder "designs"), DB lưu objectName.
    Task<DesignImageResponse> UploadFileAsync(
        long designId, Stream content, string fileName, string? contentType, long sizeBytes,
        string? caption = null, long? uploadedBy = null);
    Task RemoveFileAsync(long designId, long imageId);

    // Lịch sử version (snapshot khi submit / approve) — dùng để truy nguyên sau revision.
    // Mỗi submit / approve đều sinh 1 bản mới (full history), không upsert — có thể trả về nhiều bản.
    Task<PaginationResponse<DesignVersionResponse>> GetVersionsAsync(
        long designId, int pageNumber = 1, int pageSize = 20);
    Task<DesignVersionResponse> GetVersionByIdAsync(long designId, long versionId);
}
