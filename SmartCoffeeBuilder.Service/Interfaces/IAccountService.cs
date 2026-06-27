using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Account;
using SmartCoffeeBuilder.Service.DTOs.Responses.Account;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAccountService
{
    Task<PaginationResponse<AccountResponse>> GetAllAsync(int pageNumber = 1, int pageSize = 10);
    Task<AccountResponse> GetByIdAsync(long id);
    Task<AccountResponse> CreateAsync(CreateAccountRequest request);
    Task<AccountResponse> UpdateAsync(long id, UpdateAccountRequest request);
    Task DeleteAsync(long id);
}
