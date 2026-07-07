using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Repository.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<Role?> GetRoleByNameAsync(string name);
    Task<User> CreateUserAsync(User user, long roleId);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeAllUserRefreshTokensAsync(long userId);
}
