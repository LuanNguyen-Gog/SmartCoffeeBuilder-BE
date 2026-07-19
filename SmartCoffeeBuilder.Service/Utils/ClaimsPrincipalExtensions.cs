using System.Security.Claims;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Đọc thông tin người đăng nhập từ JWT claims.
/// Controller gọi <c>User.GetAccountId()</c> rồi truyền xuống service —
/// service KHÔNG tự đọc HttpContext (theo quy tắc phân tầng).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Lấy account id từ claim <c>sub</c> (AuthService ghi account.Id vào đây khi phát token;
    /// JwtBearer có thể map sang <see cref="ClaimTypes.NameIdentifier"/> nên check cả hai).
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Token không chứa account id hợp lệ (HTTP 401).</exception>
    public static long GetAccountId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        if (!long.TryParse(raw, out var accountId))
            throw new UnauthorizedAccessException("Token không chứa account id hợp lệ.");
        return accountId;
    }
}
