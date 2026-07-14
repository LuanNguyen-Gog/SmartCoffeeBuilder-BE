using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Contract;
using SmartCoffeeBuilder.Service.DTOs.Responses.Contract;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IContractService
{
    Task<PaginationResponse<ContractResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10, long? projectProviderId = null);

    Task<ContractResponse> GetByIdAsync(long id);

    Task<ContractResponse> CreateAsync(CreateContractRequest request);

    Task<ContractResponse> UpdateAsync(long id, UpdateContractRequest request);

    /// <summary>Sinh OTP ký hợp đồng, gửi email cho owner, chuyển drafted → pending_otp.</summary>
    Task<ContractResponse> SendOtpAsync(long id);

    /// <summary>Xác nhận OTP ký hợp đồng, chuyển pending_otp → confirmed.</summary>
    Task<ContractResponse> ConfirmOtpAsync(long id, ConfirmContractOtpRequest request);

    /// <summary>Huỷ hợp đồng khi chưa confirmed (drafted/pending_otp → cancelled).</summary>
    Task<ContractResponse> CancelAsync(long id);
}
