namespace SmartCoffeeBuilder.Service.DTOs.Responses.File;

public class FileUploadResponse
{
    /// <summary>
    /// Tên object trong bucket — ĐÂY là giá trị lưu vào DB (image_url, issue_image…),
    /// ví dụ "contracts/2026/07/abc123.pdf".
    /// </summary>
    public string ObjectName { get; set; } = null!;

    /// <summary>
    /// Đường dẫn xem file trên BE (cố định, không hết hạn):
    /// "/api/files/view?objectName=..." — FE ghép base URL của API vào trước.
    /// </summary>
    public string Url { get; set; } = null!;

    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
}

/// <summary>Kết quả đọc file từ bucket để BE stream về client.</summary>
public class FileDownloadResult
{
    public Stream Content { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string FileName { get; set; } = null!;
}
