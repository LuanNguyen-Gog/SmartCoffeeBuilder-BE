namespace SmartCoffeeBuilder.Service.DTOs.Responses.File;

public class FileUploadResponse
{
    /// <summary>
    /// Tên object trong bucket — ĐÂY là giá trị lưu vào DB (image_url, issue_image…),
    /// ví dụ "owner/12/2026/07/abc123.pdf".
    /// </summary>
    public string ObjectName { get; set; } = null!;

    /// <summary>
    /// URL public tuyệt đối trên GCS (cố định, không hết hạn), ví dụ
    /// "https://storage.googleapis.com/{bucket}/owner/12/2026/07/abc123.pdf" —
    /// FE dùng thẳng làm img src / link, xem được cả khi chưa đăng nhập.
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
