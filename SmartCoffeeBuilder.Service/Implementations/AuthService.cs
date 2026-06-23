using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.DTOs.Requests;
using SmartCoffeeBuilder.Service.DTOs.Responses;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IAuthRepository authRepository, IConfiguration configuration)
    {
        _authRepository = authRepository;
        _configuration = configuration;
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

        await _authRepository.RevokeRefreshTokenAsync(stored);

        return await IssueTokensAsync(stored.Account);
    }

    public async Task LogoutAsync(RefreshTokenRequest request)
    {
        var stored = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);
        if (stored == null || !stored.IsActive) return;

        await _authRepository.RevokeAllAccountRefreshTokensAsync(stored.AccountId);
    }

    // ──────────────────────────────────────────────────────────────
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
