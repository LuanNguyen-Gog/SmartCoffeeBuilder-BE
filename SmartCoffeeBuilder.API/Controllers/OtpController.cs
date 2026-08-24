using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Otp;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// OTP TÀI KHOẢN (quên/đặt lại mật khẩu) — CỐ Ý công khai: người quên mật khẩu chưa đăng nhập được
/// nên không thể có token. Khác OTP KÝ HỢP ĐỒNG (ContractController) vốn yêu cầu đăng nhập.
/// </summary>
[ApiController]
[Route("api/otp")]
[AllowAnonymous]
public class OtpController : ControllerBase
{
    private readonly IOtpService _otpService;

    public OtpController(IOtpService otpService)
    {
        _otpService = otpService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendOtpRequest request)
    {
        await _otpService.SendOtpAsync(request.Email);
        return Ok(new { message = "An OTP has been sent to your email." });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyOtpRequest request)
    {
        if (!await _otpService.VerifyOtpAsync(request.Email, request.Code))
            // Trả ProblemDetails cùng shape với GlobalExceptionHandler để FE xử lý lỗi đồng nhất.
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "The OTP is incorrect or has expired.");

        return Ok(new { message = "OTP verified successfully." });
    }
}
