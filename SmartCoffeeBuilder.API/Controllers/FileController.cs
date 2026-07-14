using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Upload file/ảnh lên Google Cloud Storage — bucket PRIVATE. Lưu ObjectName vào các cột
/// image_url / issue_image / confirm_image… của entity (không có bảng file riêng);
/// hiển thị qua GET api/files/view — BE stream trực tiếp từ bucket, URL cố định không hết hạn.
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileStorageService _fileStorageService;

    public FileController(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    /// <summary>Upload file (ảnh hoặc tài liệu pdf/doc/docx/xls/xlsx). Tối đa theo Gcs:MaxFileSizeMb.</summary>
    /// <param name="folder">Thư mục logic trong bucket: contracts, issues, tasks… (mặc định "uploads").</param>
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? folder = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        await using var stream = file.OpenReadStream();
        var result = await _fileStorageService.UploadAsync(
            stream, file.FileName, file.ContentType, file.Length, folder);

        return Ok(result);
    }

    /// <summary>Upload ảnh (chỉ jpg/jpeg/png/webp/gif) — dùng cho ảnh hiện trường, ảnh issue…</summary>
    [HttpPost("images")]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string? folder = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        await using var stream = file.OpenReadStream();
        var result = await _fileStorageService.UploadAsync(
            stream, file.FileName, file.ContentType, file.Length, folder, imageOnly: true);

        return Ok(result);
    }

    /// <summary>
    /// Xem/tải file — BE stream trực tiếp từ bucket private. AllowAnonymous để &lt;img src&gt;
    /// dùng thẳng được; objectName chứa GUID ngẫu nhiên nên không đoán mò được.
    /// </summary>
    [HttpGet("view")]
    [AllowAnonymous]
    public async Task<IActionResult> View([FromQuery] string objectName)
    {
        var file = await _fileStorageService.DownloadAsync(objectName);

        // Object bất biến (tên GUID, không ghi đè) — cho phép browser/CDN cache 1 ngày.
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(file.Content, file.ContentType); // không set fileDownloadName → hiển thị inline
    }

    /// <summary>Xoá file theo objectName trả về lúc upload (ví dụ: issues/2026/07/abc.png).</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string objectName)
    {
        await _fileStorageService.DeleteAsync(objectName);
        return NoContent();
    }
}
