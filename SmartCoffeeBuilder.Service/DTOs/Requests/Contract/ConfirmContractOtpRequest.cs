using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Contract;

/// <summary>Owner nhập OTP ký hợp đồng để chuyển contract → 'confirmed'.</summary>
public class ConfirmContractOtpRequest
{
    [Required]
    public string OtpCode { get; set; } = null!;

    // confirmed_by lấy từ JWT của người đang đăng nhập — KHÔNG nhận từ body:
    // chữ ký hợp đồng phải là bằng chứng do BE xác định, không để client tự khai.
}
