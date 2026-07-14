using SmartCoffeeBuilder.Service.DTOs.Responses.File;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Lưu trữ file/ảnh trên Google Cloud Storage — bucket PRIVATE. Hệ thống KHÔNG có bảng file riêng:
/// upload xong lưu ObjectName vào cột tương ứng (construction_task.image_url, issue.issue_image,
/// doc.file_url…); khi hiển thị, FE dùng GET api/files/view — BE stream trực tiếp từ bucket.
/// </summary>
public interface IFileStorageService
{
    /// <param name="content">Nội dung file.</param>
    /// <param name="fileName">Tên file gốc (lấy đuôi để validate + đặt tên object).</param>
    /// <param name="contentType">MIME type từ request.</param>
    /// <param name="sizeBytes">Kích thước file — service kiểm tra giới hạn cấu hình.</param>
    /// <param name="folder">Thư mục logic trong bucket (contracts, issues, tasks, designs…); mặc định "uploads".</param>
    /// <param name="imageOnly">true = chỉ chấp nhận định dạng ảnh.</param>
    Task<FileUploadResponse> UploadAsync(
        Stream content, string fileName, string? contentType, long sizeBytes,
        string? folder = null, bool imageOnly = false);

    /// <summary>Đọc file từ bucket để stream về client (bucket private, không dùng signed URL).</summary>
    Task<FileDownloadResult> DownloadAsync(string objectName);

    /// <summary>Xoá object theo ObjectName trả về lúc upload.</summary>
    Task DeleteAsync(string objectName);
}
