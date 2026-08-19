using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;
using SmartCoffeeBuilder.Service.DTOs.Responses.Quotation;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Báo giá tiền hợp đồng (review 3). Mọi method nhận accountId lấy từ JWT: quyền được xét theo
/// CHỖ NEO của báo giá (hồ sơ ứng tuyển hoặc engagement), không theo AccountRole — hai provider
/// cùng ứng tuyển một bài đăng đều mang role 'provider' nhưng không được đọc báo giá của nhau.
/// </summary>
public interface IQuotationService
{
    /// <summary>
    /// Danh sách báo giá đã lọc theo người gọi: owner thấy báo giá gửi cho dự án của mình,
    /// provider thấy báo giá mình gửi, admin thấy tất cả. Lọc bằng <paramref name="postId"/> để
    /// owner so sánh mọi báo giá của một bài đăng cạnh nhau.
    /// </summary>
    Task<PaginationResponse<QuotationResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? applyId = null, Guid? projectWorkingId = null, Guid? postId = null, string? status = null);

    Task<QuotationResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>Provider lập bản báo giá mới (draft) cho hồ sơ ứng tuyển hoặc engagement của mình.</summary>
    Task<QuotationResponse> CreateAsync(Guid accountId, CreateQuotationRequest request);

    /// <summary>Sửa bản nháp. Đã gửi cho owner hoặc đã khoá thì không sửa — phát hành bản mới.</summary>
    Task<QuotationResponse> UpdateAsync(Guid accountId, Guid id, UpdateQuotationRequest request);

    /// <summary>Gửi bản báo giá cho owner (draft → sent).</summary>
    Task<QuotationResponse> SendAsync(Guid accountId, Guid id);

    /// <summary>Owner yêu cầu bản khác kèm lý do (sent → revision_requested).</summary>
    Task<QuotationResponse> RequestRevisionAsync(Guid accountId, Guid id, RespondQuotationRequest request);

    /// <summary>Owner từ chối hẳn bản báo giá này (sent → rejected).</summary>
    Task<QuotationResponse> RejectAsync(Guid accountId, Guid id, RespondQuotationRequest request);

    /// <summary>
    /// Owner duyệt báo giá: khoá bản này, cho các bản còn lại cùng chỗ neo thành 'superseded',
    /// và nếu là báo giá kèm hồ sơ ứng tuyển thì chấp nhận luôn hồ sơ đó (mở engagement).
    /// </summary>
    Task<AcceptQuotationResponse> AcceptAsync(Guid accountId, Guid id);

    Task<QuotationResponse> AddAttachmentAsync(Guid accountId, Guid id, AddQuotationAttachmentRequest request);
    Task RemoveAttachmentAsync(Guid accountId, Guid id, Guid attachmentId);

    /// <summary>Provider xoá bản nháp của mình (chỉ khi còn 'draft').</summary>
    Task DeleteAsync(Guid accountId, Guid id);
}
