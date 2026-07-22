using Microsoft.Extensions.Configuration;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Chuyển giá trị file/ảnh lưu trong DB thành URL public tuyệt đối để FE dùng thẳng
/// (img src / link tải), lấy từ bucket GCS public-read.
///
/// DB lưu ObjectName ("provider/5/2026/07/abc.png"), nhưng dữ liệu cũ/seed có thể đang lưu
/// URL tuyệt đối — resolver xử lý cả hai:
/// - rỗng/null            → null
/// - "http(s)://…"        → giữ nguyên (đã là URL đầy đủ)
/// - "gs://bucket/object" → "https://storage.googleapis.com/bucket/object"
/// - còn lại (ObjectName) → "{Gcs:PublicBaseUrl}/{objectName}"
///
/// Cấu hình một lần lúc startup bằng <see cref="Configure(IConfiguration)"/> (gọi trong Program.cs).
/// Chưa cấu hình thì trả về nguyên giá trị đầu vào — không sinh URL sai.
/// </summary>
public static class MediaUrl
{
    private const string GcsHost = "https://storage.googleapis.com";

    private static string? _publicBaseUrl;
    private static string? _bucketName;

    /// <summary>Base URL public của bucket, ví dụ "https://storage.googleapis.com/{bucket}".</summary>
    public static string? PublicBaseUrl => _publicBaseUrl;

    /// <summary>
    /// Đọc "Gcs:PublicBaseUrl" (nếu có) hoặc dựng từ "Gcs:BucketName".
    /// Gọi được nhiều lần (Program.cs + constructor GcsFileStorageService) — idempotent.
    /// </summary>
    public static void Configure(IConfiguration configuration)
    {
        var bucket = configuration["Gcs:BucketName"];
        if (!string.IsNullOrWhiteSpace(bucket)) _bucketName = bucket.Trim();

        var baseUrl = configuration["Gcs:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = string.IsNullOrWhiteSpace(bucket) ? null : $"{GcsHost}/{bucket}";

        if (!string.IsNullOrWhiteSpace(baseUrl))
            _publicBaseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>Dùng cho test/host tự dựng base URL.</summary>
    public static void Configure(string publicBaseUrl)
    {
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
            _publicBaseUrl = publicBaseUrl.TrimEnd('/');
    }

    /// <summary>ObjectName (hoặc URL sẵn có) → URL public tuyệt đối; null nếu không có giá trị.</summary>
    public static string? Resolve(string? objectNameOrUrl)
    {
        if (string.IsNullOrWhiteSpace(objectNameOrUrl)) return null;

        var value = objectNameOrUrl.Trim();

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return value;

        // gs://bucket/object — bucket nằm trong chính URI nên không dùng PublicBaseUrl.
        if (value.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
            return $"{GcsHost}/{value[5..].TrimStart('/')}";

        return string.IsNullOrWhiteSpace(_publicBaseUrl)
            ? value
            : $"{_publicBaseUrl}/{value.TrimStart('/')}";
    }

    /// <summary>
    /// Chiều ngược của <see cref="Resolve(string?)"/>: lấy ObjectName trên bucket từ giá trị
    /// FE gửi lên, để BE validate/xoá được file.
    /// - ObjectName trần ("provider/4/2026/07/abc.png") → chính nó
    /// - URL public của bucket (PublicBaseUrl hoặc storage.googleapis.com/{bucket}) → phần đuôi
    /// - "gs://{bucket}/…" → phần đuôi
    /// - URL của host khác (link ngoài) → <c>false</c>, BE không quản lý file đó
    /// </summary>
    public static bool TryGetObjectName(string? objectNameOrUrl, out string objectName)
    {
        objectName = string.Empty;
        if (string.IsNullOrWhiteSpace(objectNameOrUrl)) return false;

        var value = objectNameOrUrl.Trim();

        if (value.StartsWith("gs://", StringComparison.OrdinalIgnoreCase))
        {
            var rest = value[5..].TrimStart('/');
            var slash = rest.IndexOf('/');
            if (slash <= 0) return false;

            // Bucket khác thì không phải file của hệ thống này.
            if (_bucketName != null && !rest[..slash].Equals(_bucketName, StringComparison.OrdinalIgnoreCase))
                return false;

            objectName = rest[(slash + 1)..];
            return objectName.Length > 0;
        }

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            string?[] prefixes =
            [
                _publicBaseUrl,
                _bucketName == null ? null : $"{GcsHost}/{_bucketName}"
            ];

            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrEmpty(prefix)) continue;
                if (!value.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase)) continue;

                var rest = value[(prefix.Length + 1)..];
                var query = rest.IndexOfAny(['?', '#']);
                if (query >= 0) rest = rest[..query];

                objectName = Uri.UnescapeDataString(rest);
                return objectName.Length > 0;
            }

            return false; // link ngoài — giữ nguyên, không đụng tới
        }

        objectName = value.TrimStart('/');
        return objectName.Length > 0;
    }

    /// <summary>Resolve cả danh sách (ảnh tham chiếu…); null vào → null ra.</summary>
    public static List<string>? Resolve(IEnumerable<string>? objectNamesOrUrls)
    {
        if (objectNamesOrUrls == null) return null;

        return objectNamesOrUrls
            .Select(Resolve)
            .Where(u => u != null)
            .Select(u => u!)
            .ToList();
    }
}
