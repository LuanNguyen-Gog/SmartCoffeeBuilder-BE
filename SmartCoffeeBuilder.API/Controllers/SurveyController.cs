using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Survey;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Khảo sát mặt bằng — có thể diễn ra TRƯỚC khi ký hợp đồng.
/// Chỉ cần engagement (project_provider) 'accepted' và contract_type có pha design;
/// KHÔNG yêu cầu contract 'confirmed' (khác với design/construction_item).
/// </summary>
[ApiController]
[Route("api/surveys")]
[Authorize]
public class SurveyController : ControllerBase
{
    private readonly ISurveyService _surveyService;

    public SurveyController(ISurveyService surveyService)
    {
        _surveyService = surveyService;
    }

    /// <summary>
    /// Danh sách khảo sát, đã lọc theo tầm nhìn của người gọi: chủ dự án thấy khảo sát trên dự án
    /// của mình, provider thấy bản của chính mình, admin thấy tất cả.
    ///
    /// Lọc <c>postId</c> để owner xem khảo sát của mọi provider đã ứng tuyển một bài đăng cạnh
    /// nhau mà so sánh — cùng tham số với <c>GET /api/quotations</c>, hai bên ghép lại thành màn
    /// "khảo sát + báo giá" trước khi chọn nhà cung cấp.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] Guid? applyId = null,
        [FromQuery] Guid? postId = null)
    {
        var result = await _surveyService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, projectWorkingId, applyId, postId);
        return Ok(result);
    }

    /// <summary>Chi tiết một bản khảo sát. 401 nếu không thuộc dự án của người gọi.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _surveyService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Provider tạo bản khảo sát mặt bằng. Version tự tăng 0.1 theo từng engagement (0.1, 0.2, …).
    /// </summary>
    // KHÔNG mở cho admin: khảo sát mặt bằng là việc của provider trong engagement, không phải quản trị.
    [HttpPost]
    [Authorize(Roles = "provider")]
    public async Task<IActionResult> Create([FromBody] CreateSurveyRequest request)
    {
        var result = await _surveyService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật ghi chú hiện trạng / URL báo cáo.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSurveyRequest request)
    {
        var result = await _surveyService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }
}
