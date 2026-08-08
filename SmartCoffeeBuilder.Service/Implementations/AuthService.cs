using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests.Auth;
using SmartCoffeeBuilder.Service.DTOs.Responses.Auth;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly IOtpService _otpService;

    public AuthService(IAuthRepository authRepository, IConfiguration configuration, IOtpService otpService)
    {
        _authRepository = authRepository;
        _configuration = configuration;
        _otpService = otpService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _authRepository.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new InvalidOperationException("Email đã được sử dụng.");

        if (!Enum.TryParse<AccountRole>(request.Role, ignoreCase: true, out var role))
            throw new ArgumentException($"Role '{request.Role}' không hợp lệ. Cho phép: owner, provider, admin.");

        var account = new Account
        {
            Email = request.Email,
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            Status = AccountStatus.active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _authRepository.CreateAccountAsync(account);

        return await IssueTokensAsync(account);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var account = await _authRepository.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash))
            throw new UnauthorizedAccessException("Email hoặc mật khẩu không đúng.");

        return await IssueTokensAsync(account);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request)
    {
        var stored = await _authRepository.GetRefreshTokenAsync(request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Refresh token không hợp lệ.");

        if (!stored.IsActive)
            throw new UnauthorizedAccessException("Refresh token đã hết hạn hoặc bị thu hồi.");

        if (stored.Account is null || stored.Account.DeletedAt != null)
            throw new UnauthorizedAccessException("Refresh token không hợp lệ.");

        await _authRepository.RevokeRefreshTokenAsync(stored);

        return await IssueTokensAsync(stored.Account);
    }

    public async Task LogoutAsync(RefreshTokenRequest request)
    {
        var stored = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);
        if (stored == null || !stored.IsActive) return;

        await _authRepository.RevokeAllAccountRefreshTokensAsync(stored.AccountId);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        // SendOtpAsync tự kiểm tra email: không tồn tại thì ném KeyNotFoundException
        // (GlobalExceptionHandler map thành 404), tồn tại thì gửi OTP.
        => await _otpService.SendOtpAsync(request.Email);

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var valid = await _otpService.VerifyOtpAsync(request.Email, request.Code);
        if (!valid)
            throw new ArgumentException("Mã OTP không đúng hoặc đã hết hạn.");

        var account = await _authRepository.GetByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

        account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        account.UpdatedAt = DateTime.UtcNow;
        await _authRepository.UpdateAccountAsync(account);

        // Đổi mật khẩu xong thì thu hồi mọi phiên đăng nhập cũ.
        await _authRepository.RevokeAllAccountRefreshTokensAsync(account.Id);
    }

    // ──────────────────────────────────────────────────────────────
    public async Task<MeResponse> GetMeAsync(long accountId)
    {
        // GetByIdAsync đã lọc DeletedAt == null nên tài khoản đã xoá mềm sẽ trả null → 404.
        var account = await _authRepository.GetByIdAsync(accountId)
            ?? throw new KeyNotFoundException("Tài khoản không tồn tại.");

        var response = new MeResponse
        {
            Id = account.Id,
            Email = account.Email,
            Phone = account.Phone,
            Role = account.Role.ToString(),
            Status = account.Status.ToString(),
            EmailVerifiedAt = account.EmailVerifiedAt,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };

        if (account.ShopOwner != null)
        {
            response.ShopOwner = new ShopOwnerInfo
            {
                Id = account.ShopOwner.Id,
                FullName = account.ShopOwner.FullName,
                ShopName = account.ShopOwner.ShopName,
                Phone = account.ShopOwner.Phone,
                Address = account.ShopOwner.Address
            };
        }

        if (account.ServiceProviderProfile != null)
        {
            var sp = account.ServiceProviderProfile;
            var spInfo = new ServiceProviderInfo
            {
                Id = sp.Id,
                DisplayName = sp.DisplayName,
                ProviderType = sp.ProviderType.ToString(),
                Capability = sp.Capability.ToString(),
                Bio = sp.Bio,
                CompanyTaxCode = sp.CompanyTaxCode,
                YearsExperience = sp.YearsExperience,
                PortfolioHeadline = sp.PortfolioHeadline,
                IsVerified = sp.IsVerified,
                AvgRating = sp.AvgRating,
                CreatedAt = sp.CreatedAt,
                UpdatedAt = sp.UpdatedAt
            };

            if (sp.DesignerProfile != null)
            {
                spInfo.Designer = new DesignerInfo
                {
                    Specialties = sp.DesignerProfile.Specialties,
                    SoftwareSkills = sp.DesignerProfile.SoftwareSkills,
                    DesignStyle = sp.DesignerProfile.DesignStyle,
                    MinProjectBudget = sp.DesignerProfile.MinProjectBudget
                };
            }

            if (sp.ConstructorProfile != null)
            {
                spInfo.Constructor = new ConstructorInfo
                {
                    LicenseNo = sp.ConstructorProfile.LicenseNo,
                    TeamSize = sp.ConstructorProfile.TeamSize,
                    Equipment = sp.ConstructorProfile.Equipment,
                    MaxProjectValue = sp.ConstructorProfile.MaxProjectValue,
                    WarrantyPolicy = sp.ConstructorProfile.WarrantyPolicy
                };
            }

            response.ServiceProvider = spInfo;
        }

        return response;
    }

    private async Task<AuthResponse> IssueTokensAsync(Account account)
    {
        var accessToken = GenerateAccessToken(account);
        var refreshToken = await CreateRefreshTokenAsync(account.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccountId = account.Id,
            Email = account.Email,
            Role = account.Role.ToString()
        };
    }

    private string GenerateAccessToken(Account account)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, account.Role.ToString())
        };

        var expiry = DateTime.UtcNow.AddMinutes(
            int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(long accountId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiryDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"]!);

        var refreshToken = new RefreshToken
        {
            AccountId = accountId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            CreatedAt = DateTime.UtcNow
        };

        await _authRepository.AddRefreshTokenAsync(refreshToken);
        return token;
    }
}
