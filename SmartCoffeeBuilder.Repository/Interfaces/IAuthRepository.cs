using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Repository.Interfaces;

public interface IAuthRepository
{
    Task<Account?> GetByEmailAsync(string email);
    Task<Account> CreateAccountAsync(Account account);
    Task UpdateAccountAsync(Account account);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeAllAccountRefreshTokensAsync(long accountId);
}
