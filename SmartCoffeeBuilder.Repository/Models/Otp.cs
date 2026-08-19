namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Yêu cầu OTP của một tài khoản (vd: reset mật khẩu).
/// Mã sinh bằng TOTP với key dẫn xuất từ Otp:SecretKey (appsettings) + AccountId
/// — DB không lưu secret. Mỗi mã sống 1 chu kỳ (60s).
/// CurrentCode/PreviousCode được Hangfire refresh mỗi phút để DB luôn giữ mã hiện hành.
/// </summary>
public class Otp
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }

    /// <summary>Mã TOTP của chu kỳ hiện tại.</summary>
    public string CurrentCode { get; set; } = null!;

    /// <summary>Mã của chu kỳ trước — chỉ được chấp nhận trong flex window (5s) sau khi đổi chu kỳ.</summary>
    public string? PreviousCode { get; set; }

    /// <summary>Thời điểm CurrentCode được refresh lần cuối (đầu chu kỳ TOTP).</summary>
    public DateTime CodeRefreshedAt { get; set; }

    public int FailedAttempts { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Hết hạn cả yêu cầu OTP — sau thời điểm này phải gửi lại từ đầu.</summary>
    public DateTime ExpiresAt { get; set; }

    public Account Account { get; set; } = null!;

    public bool IsActive => !IsUsed && DateTime.UtcNow < ExpiresAt;
}
