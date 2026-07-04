using System.Security.Cryptography;
using System.Text;
using OtpNet;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Sinh và kiểm tra OTP bằng TOTP (Otp.NET).
/// Secret chung nằm trong appsettings (Otp:SecretKey); key TOTP của từng tài khoản
/// được dẫn xuất bằng HMACSHA256(secret, accountId) — nhờ đó DB không phải lưu secret
/// mà mã của mỗi tài khoản vẫn khác nhau trong cùng một chu kỳ.
/// Mỗi mã sống 1 chu kỳ (stepSeconds, mặc định 60s). Sau khi sang chu kỳ mới,
/// mã của chu kỳ trước vẫn được chấp nhận thêm flexSeconds (mặc định 5s) cho QoL.
/// </summary>
public static class TotpGenerator
{
    /// <summary>Tính mã TOTP của tài khoản tại thời điểm chỉ định (mặc định là hiện tại).</summary>
    public static string ComputeCode(string appSecretKey, long accountId, int stepSeconds, int codeLength,
        DateTime? timestamp = null)
    {
        var totp = CreateTotp(appSecretKey, accountId, stepSeconds, codeLength);
        return totp.ComputeTotp(timestamp ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Kiểm tra mã: chấp nhận mã của chu kỳ hiện tại, hoặc mã của chu kỳ trước
    /// nếu mới sang chu kỳ chưa quá flexSeconds (chấp nhận cả 2 mã trong khoảng đó).
    /// </summary>
    public static bool VerifyWithFlex(string appSecretKey, long accountId, string code,
        int stepSeconds, int codeLength, int flexSeconds)
    {
        var now = DateTime.UtcNow;
        var totp = CreateTotp(appSecretKey, accountId, stepSeconds, codeLength);

        if (totp.ComputeTotp(now) == code)
            return true;

        // Số giây đã trôi qua kể từ đầu chu kỳ hiện tại (chu kỳ TOTP neo theo Unix epoch).
        var secondsIntoStep = (long)(now - DateTime.UnixEpoch).TotalSeconds % stepSeconds;
        return secondsIntoStep <= flexSeconds
               && totp.ComputeTotp(now.AddSeconds(-stepSeconds)) == code;
    }

    /// <summary>Thời điểm bắt đầu của chu kỳ TOTP hiện tại.</summary>
    public static DateTime GetCurrentStepStart(int stepSeconds)
    {
        var unixSeconds = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        return DateTime.UnixEpoch.AddSeconds(unixSeconds - unixSeconds % stepSeconds);
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>Key TOTP riêng của tài khoản = HMACSHA256(secret app, accountId).</summary>
    private static byte[] DeriveAccountKey(string appSecretKey, long accountId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecretKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(accountId.ToString()));
    }

    private static Totp CreateTotp(string appSecretKey, long accountId, int stepSeconds, int codeLength)
        => new(DeriveAccountKey(appSecretKey, accountId), step: stepSeconds, totpSize: codeLength);
}
