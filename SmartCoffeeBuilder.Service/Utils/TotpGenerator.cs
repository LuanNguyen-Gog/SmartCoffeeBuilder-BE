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
    public static string ComputeCode(string appSecretKey, Guid accountId, int stepSeconds, int codeLength,
        DateTime? timestamp = null)
    {
        var totp = CreateTotp(appSecretKey, accountId, stepSeconds, codeLength);
        return totp.ComputeTotp(timestamp ?? DateTime.UtcNow);
    }

    /// <summary>
    /// Chọn mã để GỬI cho người dùng. Nếu chu kỳ hiện tại sắp hết (còn ≤ lookAheadSeconds giây)
    /// thì trả mã của chu kỳ KẾ TIẾP để người dùng có đủ thời gian nhập — tránh trường hợp xin mã
    /// ngay sát lúc đổi chu kỳ, mã hết hạn trước khi kịp nhập. Ngược lại trả mã chu kỳ hiện tại.
    /// Trả kèm thời điểm bắt đầu chu kỳ của mã (để lưu CodeRefreshedAt) và thời điểm mã hết hạn.
    /// </summary>
    public static (string Code, DateTime StepStart, DateTime ExpiresAt) ComputeDeliveryCode(
        string appSecretKey, Guid accountId, int stepSeconds, int codeLength, int lookAheadSeconds)
    {
        if (lookAheadSeconds >= stepSeconds) lookAheadSeconds = stepSeconds - 1; // luôn còn ít nhất mã hiện tại
        if (lookAheadSeconds < 0) lookAheadSeconds = 0;

        var now = DateTime.UtcNow;
        var secondsIntoStep = (long)(now - DateTime.UnixEpoch).TotalSeconds % stepSeconds;
        var secondsRemaining = stepSeconds - secondsIntoStep;

        // Sắp hết chu kỳ hiện tại → nhảy sang chu kỳ kế tiếp.
        var useNext = secondsRemaining <= lookAheadSeconds;
        var stepStart = GetCurrentStepStart(stepSeconds);
        if (useNext) stepStart = stepStart.AddSeconds(stepSeconds);

        var code = ComputeCode(appSecretKey, accountId, stepSeconds, codeLength,
            useNext ? now.AddSeconds(stepSeconds) : now);
        return (code, stepStart, stepStart.AddSeconds(stepSeconds));
    }

    /// <summary>
    /// Kiểm tra mã: chấp nhận mã của chu kỳ hiện tại; mã của chu kỳ trước nếu mới sang chu kỳ
    /// chưa quá flexSeconds; và (khi lookAheadSeconds > 0) mã của chu kỳ KẾ TIẾP nếu chu kỳ hiện tại
    /// còn ≤ lookAheadSeconds — vì lúc đó hệ thống đã gửi trước mã chu kỳ kế tiếp cho người dùng.
    /// </summary>
    public static bool VerifyWithFlex(string appSecretKey, Guid accountId, string code,
        int stepSeconds, int codeLength, int flexSeconds, int lookAheadSeconds = 0)
    {
        var now = DateTime.UtcNow;
        var totp = CreateTotp(appSecretKey, accountId, stepSeconds, codeLength);

        // Mã chu kỳ hiện tại.
        if (totp.ComputeTotp(now) == code)
            return true;

        // Số giây đã trôi qua kể từ đầu chu kỳ hiện tại (chu kỳ TOTP neo theo Unix epoch).
        var secondsIntoStep = (long)(now - DateTime.UnixEpoch).TotalSeconds % stepSeconds;

        // Mã chu kỳ trước — chấp nhận trong flexSeconds đầu chu kỳ mới.
        if (secondsIntoStep <= flexSeconds && totp.ComputeTotp(now.AddSeconds(-stepSeconds)) == code)
            return true;

        // Mã chu kỳ kế tiếp — chấp nhận trong lookAheadSeconds cuối chu kỳ hiện tại (đã gửi trước cho user).
        var secondsRemaining = stepSeconds - secondsIntoStep;
        if (lookAheadSeconds > 0 && secondsRemaining <= lookAheadSeconds
            && totp.ComputeTotp(now.AddSeconds(stepSeconds)) == code)
            return true;

        return false;
    }

    /// <summary>Thời điểm bắt đầu của chu kỳ TOTP hiện tại.</summary>
    public static DateTime GetCurrentStepStart(int stepSeconds)
    {
        var unixSeconds = (long)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds;
        return DateTime.UnixEpoch.AddSeconds(unixSeconds - unixSeconds % stepSeconds);
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>Key TOTP riêng của tài khoản = HMACSHA256(secret app, accountId).</summary>
    private static byte[] DeriveAccountKey(string appSecretKey, Guid accountId)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(appSecretKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(accountId.ToString()));
    }

    private static Totp CreateTotp(string appSecretKey, Guid accountId, int stepSeconds, int codeLength)
        => new(DeriveAccountKey(appSecretKey, accountId), step: stepSeconds, totpSize: codeLength);
}
