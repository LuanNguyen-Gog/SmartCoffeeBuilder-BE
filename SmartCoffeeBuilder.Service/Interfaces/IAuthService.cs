using SmartCoffeeBuilder.Service.DTOs.Requests.Auth;
using SmartCoffeeBuilder.Service.DTOs.Responses.Auth;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request);
    Task LogoutAsync(RefreshTokenRequest request);
}
