using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.PaymentBatch;
using SmartCoffeeBuilder.Service.DTOs.Responses.PaymentBatch;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Đợt thanh toán owner → provider của một hợp đồng (review 3).
///
/// Hệ thống KHÔNG giữ tiền: owner tự chuyển khoản rồi upload minh chứng, provider xác nhận đã nhận.
/// Các đợt được SINH TỰ ĐỘNG từ điều kiện thanh toán của báo giá khi hợp đồng được ký — không có
/// endpoint tạo tay, để bảng đợt tiền luôn khớp với cam kết hai bên đã chốt.
/// </summary>
public interface IPaymentBatchService
{
    Task<PaginationResponse<PaymentBatchResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? contractId = null, Guid? projectWorkingId = null, string? status = null);

    Task<PaymentBatchResponse> GetByIdAsync(Guid accountId, Guid id);

    /// <summary>Owner upload minh chứng đã chuyển tiền cho đợt này (pending/rejected → proof_submitted).</summary>
    Task<PaymentBatchResponse> SubmitProofAsync(Guid accountId, Guid id, SubmitPaymentProofRequest request);

    /// <summary>
    /// Provider xác nhận đã nhận đủ tiền (proof_submitted → confirmed). Hạng mục thi công gắn với
    /// đợt này (nếu có) được đánh dấu đã thanh toán.
    /// </summary>
    Task<PaymentBatchResponse> ConfirmAsync(Guid accountId, Guid id);

    /// <summary>Provider bác minh chứng kèm lý do (proof_submitted → rejected) — owner upload lại.</summary>
    Task<PaymentBatchResponse> RejectAsync(Guid accountId, Guid id, RejectPaymentBatchRequest request);

    /// <summary>Provider gắn/gỡ hạng mục thi công tương ứng với đợt thanh toán.</summary>
    Task<PaymentBatchResponse> LinkConstructionItemAsync(
        Guid accountId, Guid id, LinkConstructionItemRequest request);
}
