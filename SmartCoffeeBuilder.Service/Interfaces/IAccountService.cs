using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Account;
using SmartCoffeeBuilder.Service.DTOs.Responses.Account;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAccountService
{
    Task<PaginationResponse<AccountResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<AccountResponse> GetByIdAsync(Guid id);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request);
    Task<AccountResponse> UpdateAsync(Guid id, UpdateAccountRequest request);
    Task DeleteAsync(Guid id);

    /// <summary>Admin: liệt kê tài khoản có lọc theo role/status/từ khoá (email hoặc phone).</summary>
    Task<PaginationResponse<AccountResponse>> SearchAsync(
        int pageNumber = 1, int pageSize = 10,
        string? role = null, string? status = null, string? search = null, bool includeDeleted = false);

    /// <summary>Admin: đổi trạng thái tài khoản (active/inactive/banned/pending).</summary>
    Task<AccountResponse> SetStatusAsync(Guid id, string status);
}
