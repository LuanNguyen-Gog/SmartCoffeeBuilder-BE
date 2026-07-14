namespace SmartCoffeeBuilder.Service.DTOs.Responses.File;

public class FileUploadResponse
{
    /// <summary>
    /// Tên object trong bucket — ĐÂY là giá trị lưu vào DB (image_url, issue_image…),
    /// ví dụ "contracts/2026/07/abc123.pdf". Muốn xem file gọi GET api/files/signed-url.
    /// </summary>
    public string ObjectName { get; set; } = null!;

    /// <summary>Signed URL tạm thời để xem/tải file ngay — KHÔNG lưu vào DB vì sẽ hết hạn.</summary>
    public string Url { get; set; } = null!;

    /// <summary>Thời điểm (UTC) signed URL hết hạn.</summary>
    public DateTime UrlExpiresAt { get; set; }

    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
}

public class SignedUrlResponse
{
    public string ObjectName { get; set; } = null!;
    public string Url { get; set; } = null!;
    public DateTime UrlExpiresAt { get; set; }
}
