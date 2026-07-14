using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Contract;

/// <summary>Owner nhập OTP ký hợp đồng để chuyển contract → 'confirmed'.</summary>
public class ConfirmContractOtpRequest
{
    [Required]
    public string OtpCode { get; set; } = null!;

    /// <summary>Account id của owner xác nhận (ghi vào confirmed_by).</summary>
    [Required]
    public long ConfirmedBy { get; set; }
}
