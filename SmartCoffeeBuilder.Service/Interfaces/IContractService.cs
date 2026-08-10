using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Contract;
using SmartCoffeeBuilder.Service.DTOs.Responses.Contract;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IContractService
{
    /// <summary>
    /// Danh sách hợp đồng người gọi được phép thấy: owner chỉ thấy hợp đồng của dự án mình,
    /// provider chỉ thấy hợp đồng của engagement mình, admin thấy tất cả.
    /// <paramref name="accountId"/> lấy từ JWT — KHÔNG nhận từ query/body.
    /// </summary>
    Task<PaginationResponse<ContractResponse>> GetAllAsync(
        long accountId, int pageNumber = 1, int pageSize = 10, long? projectWorkingId = null);

    /// <summary>
    /// Chi tiết một hợp đồng. Người gọi phải là owner của dự án hoặc provider của chính engagement
    /// mang hợp đồng đó (admin xem được tất cả) — sai người trả 401.
    /// </summary>
    Task<ContractResponse> GetByIdAsync(long accountId, long id);

    /// <summary>Provider của engagement soạn bản hợp đồng (draft). Bên khác gọi → 401.</summary>
    Task<ContractResponse> CreateAsync(long accountId, CreateContractRequest request);

    /// <summary>Provider của engagement sửa nội dung khi còn 'drafted'. Bên khác gọi → 401.</summary>
    Task<ContractResponse> UpdateAsync(long accountId, long id, UpdateContractRequest request);

    /// <summary>
    /// Provider của engagement phát OTP ký hợp đồng, gửi email cho owner, chuyển drafted → pending_otp.
    /// Quyền kiểm TRƯỚC khi phát mã — endpoint này vừa gửi email vừa đổi trạng thái.
    /// </summary>
    Task<ContractResponse> SendOtpAsync(long accountId, long id);

    /// <summary>
    /// Owner xác nhận OTP ký hợp đồng, chuyển pending_otp → confirmed.
    /// <paramref name="accountId"/> lấy từ JWT — phải là owner của dự án, và chính account này
    /// được ghi vào confirmed_by (không nhận từ body để chữ ký không giả mạo được).
    /// </summary>
    Task<ContractResponse> ConfirmOtpAsync(long accountId, long id, ConfirmContractOtpRequest request);

    /// <summary>
    /// Huỷ hợp đồng khi chưa confirmed (drafted/pending_otp → cancelled).
    /// Cả hai bên của engagement đều huỷ được; người ngoài → 401.
    /// </summary>
    Task<ContractResponse> CancelAsync(long accountId, long id);
}
