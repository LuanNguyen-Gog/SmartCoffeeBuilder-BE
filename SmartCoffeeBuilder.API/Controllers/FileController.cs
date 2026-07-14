using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Upload file/ảnh lên Google Cloud Storage — bucket PUBLIC-READ. Không nhận folder từ client:
/// object được lưu theo "{role}/{accountId}/{yyyy}/{MM}/{guid}{ext}" lấy từ JWT.
/// Response trả Url public tuyệt đối (https://storage.googleapis.com/...) — FE dùng thẳng,
/// xem được cả khi chưa đăng nhập. Lưu ObjectName vào các cột image_url / issue_image /
/// confirm_image… của entity (không có bảng file riêng).
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

    /// <summary>Dựng đường dẫn thư mục "{role}/{accountId}" từ claims của token.</summary>
    private string GetUploaderFolderPath()
    {
        var accountId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        var role = User.FindFirstValue(ClaimTypes.Role)
            ?? throw new UnauthorizedAccessException("Role not found in token");

        return $"{role}/{accountId}";
    }

    /// <summary>Upload file (ảnh hoặc tài liệu pdf/doc/docx/xls/xlsx). Tối đa theo Gcs:MaxFileSizeMb.</summary>
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        await using var stream = file.OpenReadStream();
        var result = await _fileStorageService.UploadAsync(
            stream, file.FileName, file.ContentType, file.Length, GetUploaderFolderPath());

        return Ok(result);
    }

    /// <summary>Upload ảnh (chỉ jpg/jpeg/png/webp/gif) — dùng cho ảnh hiện trường, ảnh issue…</summary>
    [HttpPost("images")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Chưa chọn file hoặc file rỗng.");

        await using var stream = file.OpenReadStream();
        var result = await _fileStorageService.UploadAsync(
            stream, file.FileName, file.ContentType, file.Length, GetUploaderFolderPath(), imageOnly: true);

        return Ok(result);
    }

    /// <summary>
    /// Xem/tải file — BE stream từ bucket. GIỮ để tương thích dữ liệu cũ (DB đang lưu objectName);
    /// file mới FE dùng thẳng Url public trả về lúc upload.
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
