using SmartCoffeeBuilder.Service.DTOs.Responses.File;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Lưu trữ file/ảnh trên Google Cloud Storage — bucket PUBLIC-READ (allUsers: Storage Object Viewer).
/// Hệ thống KHÔNG có bảng file riêng: upload xong lưu ObjectName vào cột tương ứng
/// (construction_task.image_url, issue.issue_image, doc.file_url…); FE hiển thị bằng Url public
/// trả về lúc upload (https://storage.googleapis.com/...), xem được cả khi chưa đăng nhập.
/// </summary>
public interface IFileStorageService
{
    /// <param name="content">Nội dung file.</param>
    /// <param name="fileName">Tên file gốc (lấy đuôi để validate + đặt tên object).</param>
    /// <param name="contentType">MIME type từ request.</param>
    /// <param name="sizeBytes">Kích thước file — service kiểm tra giới hạn cấu hình.</param>
    /// <param name="folderPath">
    /// Đường dẫn thư mục trong bucket, cho phép nhiều cấp ("owner/12"). Caller tự dựng
    /// (FileController dựng "{role}/{accountId}" từ JWT); service tự nối thêm năm/tháng.
    /// </param>
    /// <param name="imageOnly">true = chỉ chấp nhận định dạng ảnh.</param>
    Task<FileUploadResponse> UploadAsync(
        Stream content, string fileName, string? contentType, long sizeBytes,
        string folderPath, bool imageOnly = false);

    /// <summary>Đọc file từ bucket để stream về client (bucket private, không dùng signed URL).</summary>
    Task<FileDownloadResult> DownloadAsync(string objectName);

    /// <summary>Xoá object theo ObjectName trả về lúc upload.</summary>
    Task DeleteAsync(string objectName);

    /// <summary>
    /// ObjectName lưu trong DB → URL public tuyệt đối để FE hiển thị/tải
    /// ("https://storage.googleapis.com/{bucket}/{objectName}"). Giá trị đã là URL đầy đủ
    /// (dữ liệu cũ) được giữ nguyên; null/rỗng trả về null.
    /// </summary>
    string? GetPublicUrl(string? objectName);

    /// <summary>Object có thật trên bucket không — dùng để validate trước khi lưu vào DB.</summary>
    Task<bool> ExistsAsync(string objectName);

    /// <summary>
    /// Chuẩn hoá giá trị file FE gửi lên trước khi lưu DB (dùng cho các entity chỉ lưu chuỗi:
    /// construction_task.image_url, issue.issue_image/confirm_image, survey.report_url,
    /// contract.document_url):
    /// - URL public của bucket → rút về ObjectName (để BE xoá/quản lý được về sau)
    /// - ObjectName → giữ nguyên, nhưng BẮT BUỘC phải tồn tại trên bucket
    /// - Link ngoài (host khác) → giữ nguyên, không kiểm tra
    /// - null/rỗng → null
    /// </summary>
    /// <param name="fieldName">Tên field trong request, để báo lỗi cho FE dễ hiểu.</param>
    /// <exception cref="ArgumentException">Object không tồn tại trên bucket (HTTP 400).</exception>
    Task<string?> NormalizeForStorageAsync(string? objectNameOrUrl, string fieldName);

    /// <summary>
    /// Xoá file khi record bị xoá hoặc field bị ghi đè — best-effort: bỏ qua link ngoài,
    /// bỏ qua object đã biến mất. Gọi SAU khi DB đã commit.
    /// </summary>
    Task TryDeleteAsync(string? objectNameOrUrl);
}
