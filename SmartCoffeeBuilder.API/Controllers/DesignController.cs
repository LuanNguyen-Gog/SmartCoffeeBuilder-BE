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
        [FromQuery] Guid? projectWorkingId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null)
    {
        var result = await _designService.GetAllAsync(
            User.GetAccountId(), pageNumber, pageSize, projectWorkingId, status, type);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _designService.GetByIdAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Provider tạo bản design mới (status=in_progress, version=0.1).
    /// Engagement phải 'accepted', có contract 'confirmed' và contract type design/both.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Create([FromBody] CreateDesignRequest request)
    {
        var result = await _designService.CreateAsync(User.GetAccountId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Cập nhật title/type — chỉ khi design đang 'in_progress' hoặc 'revision'.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDesignRequest request)
    {
        var result = await _designService.UpdateAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>[SUBMIT] Provider nộp bản design cho owner duyệt (in_progress → submitted). Phải có ít nhất 1 ảnh.</summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var result = await _designService.SubmitAsync(id, User.GetAccountId());
        return Ok(result);
    }

    /// <summary>[APPROVE] Owner duyệt bản design (submitted → approved).</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _designService.ApproveAsync(id, User.GetAccountId());
        return Ok(result);
    }

    /// <summary>[REVISION] Owner yêu cầu chỉnh sửa kèm lý do (submitted → revision).</summary>
    [HttpPost("{id:guid}/request-revision")]
    [Authorize(Roles = "owner,admin")]
    public async Task<IActionResult> RequestRevision(Guid id, [FromBody] RequestDesignRevisionRequest request)
    {
        var result = await _designService.RequestRevisionAsync(User.GetAccountId(), id, request);
        return Ok(result);
    }

    /// <summary>[REWORK] Provider bắt đầu sửa theo yêu cầu (revision → in_progress, version +0.1).</summary>
    [HttpPost("{id:guid}/start-revision")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> StartRevision(Guid id)
    {
        var result = await _designService.StartRevisionAsync(User.GetAccountId(), id);
        return Ok(result);
    }

    /// <summary>
    /// Upload ảnh render hoặc file bản vẽ (pdf/office) cho design — lưu lên GCS theo đúng quy ước
    /// của api/files: object nằm ở "{role}/{accountId}/{yyyy}/{MM}/{guid}{ext}".
    /// Multipart form-data: file (bắt buộc), caption. Không thêm được khi design đã approved.
    /// </summary>
    [HttpPost("{id:guid}/files")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> UploadFile(
        Guid id, IFormFile file,
        [FromForm] string? caption = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        var accountId = User.GetAccountId();

        await using var stream = file.OpenReadStream();
        // Người upload luôn là người đang đăng nhập — KHÔNG nhận uploadedBy từ form, nếu không
        // cột uploaded_by và folder "{role}/{accountId}" đều do client tự khai.
        var result = await _designService.UploadFileAsync(
            accountId, id, stream, file.FileName, file.ContentType, file.Length, caption, accountId);

        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    /// <summary>Xóa file khỏi design (xoá cả object trên bucket) — không xóa được khi đã approved.</summary>
    [HttpDelete("{id:guid}/files/{fileId:guid}")]
    [Authorize(Roles = "provider,admin")]
    public async Task<IActionResult> RemoveFile(Guid id, Guid fileId)
    {
        await _designService.RemoveFileAsync(User.GetAccountId(), id, fileId);
        return NoContent();
    }

    /// <summary>
    /// Danh sách version (snapshot) của design — full history: mỗi submit / approve / request-revision
    /// đều sinh 1 bản mới. Bản 'revision' giữ LÝ DO của đúng vòng sửa đó kèm bộ ảnh lúc owner trả về.
    /// Phân trang theo pageNumber / pageSize (mặc định 1 / 20).
    /// </summary>
    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _designService.GetVersionsAsync(User.GetAccountId(), id, pageNumber, pageSize);
        return Ok(result);
    }

    /// <summary>Chi tiết 1 version kèm ảnh snapshot.</summary>
    [HttpGet("{id:guid}/versions/{versionId:guid}")]
    public async Task<IActionResult> GetVersion(Guid id, Guid versionId)
    {
        var result = await _designService.GetVersionByIdAsync(User.GetAccountId(), id, versionId);
        return Ok(result);
    }
}
