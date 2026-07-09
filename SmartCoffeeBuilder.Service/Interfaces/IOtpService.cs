namespace SmartCoffeeBuilder.Service.Interfaces;

public interface IOtpService
{
    /// <summary>
    /// Sinh OTP (TOTP) cho tài khoản có email tương ứng, lưu vào bảng otps rồi gửi qua Gmail.
    /// Nếu đã có yêu cầu còn hiệu lực thì gửi lại mã của chu kỳ hiện tại (không tạo dòng mới).
    /// Ném KeyNotFoundException nếu email không thuộc tài khoản nào.
    /// </summary>
    Task SendOtpAsync(string email);

    /// <summary>
    /// Kiểm tra OTP: chấp nhận mã của chu kỳ hiện tại (60s), hoặc mã chu kỳ trước
    /// trong 5s đầu của chu kỳ mới (flex window). Đúng thì đánh dấu đã dùng (một lần).
    /// Sai quá số lần cho phép thì yêu cầu OTP bị vô hiệu.
    /// </summary>
    Task<bool> VerifyOtpAsync(string email, string code);

    /// <summary>
    /// Job chạy nền (Hangfire, mỗi phút): refresh CurrentCode/PreviousCode của các OTP
    /// còn hiệu lực theo chu kỳ TOTP mới, và xoá các OTP đã dùng / hết hạn.
    /// </summary>
    Task RefreshOtpsAsync();
}
