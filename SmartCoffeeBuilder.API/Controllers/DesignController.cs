using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.DTOs.Requests.Design;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Bản design của giai đoạn DESIGN, với vòng duyệt/revision:
/// in_progress → submitted → approved, hoặc submitted → revision → in_progress → submitted…
/// Engagement giữ 'accepted' suốt quá trình; pha design là derived từ
/// contract_type + trạng thái các design con (không đổi provider_status).
/// </summary>
[ApiController]
[Route("api/designs")]
[Authorize]
public class DesignController : ControllerBase
{
    private readonly IDesignService _designService;

    public DesignController(IDesignService designService)
    {
        _designService = designService;
    }

    /// <summary>
    /// Danh sách design. status: in_progress | submitted | revision | approved.
    /// type: concept | layout_2d | render_3d | technical_drawing.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] long? projectWorkingId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var result = await _designService.GetAllAsync(pageNumber, pageSize, projectWorkingId, status, type);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _designService.GetByIdAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Provider tạo bản design mới (status=in_progress, version=0.1).
    /// Engagement phải 'accepted', có contract 'confirmed' và contract type design/both.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDesignRequest request)
    {
        var result = await _designService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật title/type — chỉ khi design đang 'in_progress' hoặc 'revision'.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDesignRequest request)
    {
        var result = await _designService.UpdateAsync(id, request);
        return Ok(result);
    }

    /// <summary>[SUBMIT] Provider nộp bản design cho owner duyệt (in_progress → submitted). Phải có ít nhất 1 ảnh.</summary>
    [HttpPost("{id:long}/submit")]
    public async Task<IActionResult> Submit(long id)
    {
        var result = await _designService.SubmitAsync(id, User.GetAccountId());
        return Ok(result);
    }

    /// <summary>[APPROVE] Owner duyệt bản design (submitted → approved).</summary>
    [HttpPost("{id:long}/approve")]
    public async Task<IActionResult> Approve(long id)
    {
        var result = await _designService.ApproveAsync(id, User.GetAccountId());
        return Ok(result);
    }

    /// <summary>[REVISION] Owner yêu cầu chỉnh sửa kèm lý do (submitted → revision).</summary>
    [HttpPost("{id:long}/request-revision")]
    public async Task<IActionResult> RequestRevision(long id, [FromBody] RequestDesignRevisionRequest request)
    {
        var result = await _designService.RequestRevisionAsync(id, request);
        return Ok(result);
    }

    /// <summary>[REWORK] Provider bắt đầu sửa theo yêu cầu (revision → in_progress, version +0.1).</summary>
    [HttpPost("{id:long}/start-revision")]
    public async Task<IActionResult> StartRevision(long id)
    {
        var result = await _designService.StartRevisionAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Upload ảnh render hoặc file bản vẽ (pdf/office) cho design — lưu lên GCS theo đúng quy ước
    /// của api/files: object nằm ở "{role}/{accountId}/{yyyy}/{MM}/{guid}{ext}".
    /// Multipart form-data: file (bắt buộc), caption, uploadedBy (mặc định = người đang đăng nhập).
    /// Không thêm được khi design đã approved.
    /// </summary>
    [HttpPost("{id:long}/files")]
    public async Task<IActionResult> UploadFile(
        long id, IFormFile file,
        [FromForm] string? caption = null,
        [FromForm] long? uploadedBy = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        await using var stream = file.OpenReadStream();
        var result = await _designService.UploadFileAsync(
            id, stream, file.FileName, file.ContentType, file.Length, caption,
            // Không có uploadedBy thì lấy account từ token (giống FileController) — luôn có
            // folder "{role}/{accountId}", không rơi vào folder chung.
            uploadedBy ?? User.GetAccountId());

        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    /// <summary>Xóa file khỏi design (xoá cả object trên bucket) — không xóa được khi đã approved.</summary>
    [HttpDelete("{id:long}/files/{fileId:long}")]
    public async Task<IActionResult> RemoveFile(long id, long fileId)
    {
        await _designService.RemoveFileAsync(id, fileId);
        return NoContent();
    }

    /// <summary>
    /// Danh sách version (snapshot) của design — full history (mỗi submit/approve đều sinh 1 bản mới).
    /// Phân trang theo pageNumber / pageSize (mặc định 1 / 20).
    /// </summary>
    [HttpGet("{id:long}/versions")]
    public async Task<IActionResult> GetVersions(
        long id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _designService.GetVersionsAsync(id, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>Chi tiết 1 version kèm ảnh snapshot.</summary>
    [HttpGet("{id:long}/versions/{versionId:long}")]
    public async Task<IActionResult> GetVersion(long id, long versionId)
    {
        var result = await _designService.GetVersionByIdAsync(id, versionId);
        return Ok(result);
    }
}
