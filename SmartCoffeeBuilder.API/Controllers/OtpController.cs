using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Otp;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

[ApiController]
[Route("api/otp")]
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
        return Ok(new { message = "Đã gửi mã OTP tới email." });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] VerifyOtpRequest request)
    {
        if (!await _otpService.VerifyOtpAsync(request.Email, request.Code))
            // Trả ProblemDetails cùng shape với GlobalExceptionHandler để FE xử lý lỗi đồng nhất.
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad Request",
                detail: "Mã OTP không đúng hoặc đã hết hạn.");

        return Ok(new { message = "Xác thực OTP thành công." });
    }
}
