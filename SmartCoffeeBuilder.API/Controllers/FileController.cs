using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Upload file/ảnh lên Google Cloud Storage — bucket PRIVATE. Lưu ObjectName vào các cột
/// image_url / issue_image / confirm_image… của entity (không có bảng file riêng);
/// khi cần hiển thị, gọi GET api/files/url để lấy link xem có hạn dùng.
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

    /// <summary>Lấy URL xem file private, có hạn dùng (expiryMinutes mặc định theo config, tối đa 7 ngày).</summary>
    [HttpGet("url")]
    public async Task<IActionResult> GetSignedUrl(
        [FromQuery] string objectName,
        [FromQuery] int? expiryMinutes = null)
    {
        var result = await _fileStorageService.GetSignedUrlAsync(objectName, expiryMinutes);
        return Ok(result);
    }

    /// <summary>Xoá file theo objectName trả về lúc upload (ví dụ: issues/2026/07/abc.png).</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string objectName)
    {
        await _fileStorageService.DeleteAsync(objectName);
        return NoContent();
    }
}
