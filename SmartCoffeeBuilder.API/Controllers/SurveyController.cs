using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Survey;
using SmartCoffeeBuilder.Service.Interfaces;

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

    /// <summary>Danh sách survey, lọc theo engagement.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectProviderId = null)
    {
        var result = await _surveyService.GetAllAsync(pageNumber, pageSize, projectProviderId);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _surveyService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Provider tạo bản khảo sát mặt bằng. Version tự tăng 0.1 theo từng engagement (0.1, 0.2, …).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSurveyRequest request)
    {
        var result = await _surveyService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật ghi chú hiện trạng / URL báo cáo.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSurveyRequest request)
    {
        var result = await _surveyService.UpdateAsync(id, request);
        return Ok(result);
    }
}
