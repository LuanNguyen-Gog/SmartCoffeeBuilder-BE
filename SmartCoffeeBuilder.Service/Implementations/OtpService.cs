using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Quản lý vòng đời OTP bằng TOTP (Otp.NET) + bảng otps:
/// key dẫn xuất từ Otp:SecretKey (appsettings) + AccountId → lưu mã vào DB
/// → email mã của chu kỳ hiện tại → verify theo TOTP.
/// Mỗi mã sống 1 chu kỳ (Otp:StepSeconds = 60s); mã chu kỳ trước được chấp nhận
/// thêm Otp:FlexSeconds (5s) sau khi đổi chu kỳ. Hangfire gọi RefreshOtpsAsync
/// mỗi phút để cập nhật mã trong DB và dọn các OTP đã dùng / hết hạn.
/// Khi gửi, nếu chu kỳ hiện tại còn ≤ Otp:LookAheadSeconds (30s) thì gửi luôn mã chu kỳ
/// KẾ TIẾP (và verify cũng chấp nhận mã đó) để tránh mã hết hạn trước khi người dùng kịp nhập.
/// </summary>
public class OtpService : IOtpService
{
    private const int MaxVerifyAttempts = 5;

    private readonly IOtpRepository _otpRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IOtpRepository otpRepository,
        IAuthRepository authRepository,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<OtpService> logger)
    {
        _otpRepository = otpRepository;
        _authRepository = authRepository;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    private string AppSecretKey => _configuration["Otp:SecretKey"]
        ?? throw new InvalidOperationException("Missing configuration: Otp:SecretKey");
    private int CodeLength => int.Parse(_configuration["Otp:Length"] ?? "6");
    private int StepSeconds => int.Parse(_configuration["Otp:StepSeconds"] ?? "60");
    private int FlexSeconds => int.Parse(_configuration["Otp:FlexSeconds"] ?? "5");
    private int RequestExpiryMinutes => int.Parse(_configuration["Otp:RequestExpiryMinutes"] ?? "5");
    // Nếu chu kỳ hiện tại còn ≤ ngần này giây thì gửi mã chu kỳ kế tiếp (tránh mã hết hạn ngay khi vừa gửi).
    private int LookAheadSeconds => int.Parse(_configuration["Otp:LookAheadSeconds"] ?? "30");

    public async Task SendOtpAsync(string email)
    {
        var account = await _authRepository.GetByEmailAsync(email)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản với email này.");

        // Tái sử dụng yêu cầu còn hiệu lực (bấm gửi lại) — chỉ gửi mã chu kỳ hiện tại.
        var otp = await _otpRepository.GetActiveAsync(account.Id);
        if (otp is null)
        {
            otp = new Otp
            {
                AccountId = account.Id,
                CurrentCode = string.Empty, // gán ngay bên dưới
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(RequestExpiryMinutes)
            };
            SetDeliveryCode(otp);
            await _otpRepository.AddAsync(otp);
        }
        else
        {
            SetDeliveryCode(otp);
            await _otpRepository.SaveChangesAsync();
        }

        await _emailService.SendTemplateAsync(
            email,
            subject: "Mã xác thực OTP - Smart Coffee Builder",
            templateName: "OtpEmail",
            placeholders: new Dictionary<string, string>
            {
                ["OtpCode"] = otp.CurrentCode,
                ["ExpiryMinutes"] = $"{StepSeconds / 60}",
                ["Year"] = DateTime.UtcNow.Year.ToString()
            });

        _logger.LogInformation("Đã gửi OTP tới {Email}", email);
    }

    public async Task<bool> VerifyOtpAsync(string email, string code)
    {
        var account = await _authRepository.GetByEmailAsync(email);
        if (account is null) return false;

        var otp = await _otpRepository.GetActiveAsync(account.Id);
        if (otp is null) return false; // chưa gửi hoặc đã hết hạn / đã dùng

        // Verify tính TOTP trực tiếp từ secret + accountId (không phụ thuộc job refresh chạy đúng giờ):
        // nhận mã chu kỳ hiện tại, hoặc mã chu kỳ trước trong FlexSeconds đầu chu kỳ mới.
        if (!TotpGenerator.VerifyWithFlex(AppSecretKey, account.Id, code, StepSeconds, CodeLength, FlexSeconds, LookAheadSeconds))
        {
            otp.FailedAttempts++;
            if (otp.FailedAttempts >= MaxVerifyAttempts)
            {
                // Chặn brute-force: sai quá số lần cho phép thì vô hiệu yêu cầu OTP.
                otp.IsUsed = true;
                _logger.LogWarning("OTP của {Email} bị vô hiệu do nhập sai quá {Max} lần",
                    email, MaxVerifyAttempts);
            }
            await _otpRepository.SaveChangesAsync();
            return false;
        }

        // Mã dùng một lần — verify thành công thì đánh dấu đã dùng.
        otp.IsUsed = true;
        await _otpRepository.SaveChangesAsync();
        return true;
    }

    public async Task RefreshOtpsAsync()
    {
        // Dọn các OTP đã dùng hoặc hết hạn.
        var deleted = await _otpRepository.DeleteExpiredOrUsedAsync();

        // Refresh mã của các OTP còn hiệu lực sang chu kỳ TOTP hiện tại.
        var stepStart = TotpGenerator.GetCurrentStepStart(StepSeconds);
        var activeOtps = await _otpRepository.GetAllActiveAsync();
        var refreshed = 0;

        foreach (var otp in activeOtps)
        {
            if (otp.CodeRefreshedAt >= stepStart) continue; // vẫn trong chu kỳ cũ, chưa cần refresh

            RefreshCodes(otp);
            refreshed++;
        }

        if (refreshed > 0)
            await _otpRepository.SaveChangesAsync();

        if (deleted > 0 || refreshed > 0)
            _logger.LogInformation("OTP refresh job: {Refreshed} mã được refresh, {Deleted} dòng bị xoá",
                refreshed, deleted);
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Gán mã để GỬI cho người dùng: nếu chu kỳ hiện tại sắp hết (còn ≤ LookAheadSeconds giây)
    /// thì dùng mã chu kỳ KẾ TIẾP để người dùng có đủ thời gian nhập. Giữ mã cũ vào PreviousCode.
    /// </summary>
    private void SetDeliveryCode(Otp otp)
    {
        var (code, stepStart, _) = TotpGenerator.ComputeDeliveryCode(
            AppSecretKey, otp.AccountId, StepSeconds, CodeLength, LookAheadSeconds);
        if (code == otp.CurrentCode) return;

        otp.PreviousCode = string.IsNullOrEmpty(otp.CurrentCode) ? null : otp.CurrentCode;
        otp.CurrentCode = code;
        otp.CodeRefreshedAt = stepStart;
    }

    /// <summary>Cập nhật CurrentCode theo chu kỳ TOTP hiện tại, giữ mã cũ vào PreviousCode.</summary>
    private void RefreshCodes(Otp otp)
    {
        var newCode = TotpGenerator.ComputeCode(AppSecretKey, otp.AccountId, StepSeconds, CodeLength);
        if (newCode == otp.CurrentCode) return;

        otp.PreviousCode = string.IsNullOrEmpty(otp.CurrentCode) ? null : otp.CurrentCode;
        otp.CurrentCode = newCode;
        otp.CodeRefreshedAt = TotpGenerator.GetCurrentStepStart(StepSeconds);
    }
}
