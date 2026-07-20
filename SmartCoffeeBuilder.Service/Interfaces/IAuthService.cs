using SmartCoffeeBuilder.Service.DTOs.Requests.Auth;
using SmartCoffeeBuilder.Service.DTOs.Responses.Auth;

namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request);
    Task LogoutAsync(RefreshTokenRequest request);

    /// <summary>Lấy thông tin account hiện tại kèm profile chi tiết (ShopOwner / ServiceProvider).</summary>
    Task<MeResponse> GetMeAsync(long accountId);

    /// <summary>Gửi OTP reset mật khẩu tới email (im lặng bỏ qua nếu email không tồn tại).</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request);

    /// <summary>Verify OTP rồi đổi mật khẩu; thu hồi toàn bộ refresh token của tài khoản.</summary>
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
