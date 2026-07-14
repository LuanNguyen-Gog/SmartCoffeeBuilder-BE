using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using SmartCoffeeBuilder.Service.DTOs.Responses.File;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Upload/xoá file trên Google Cloud Storage — bucket PRIVATE (Public access prevention bật).
/// DB chỉ lưu objectName; muốn xem file thì xin signed URL có hạn dùng qua GetSignedUrlAsync.
///
/// Config (appsettings):
/// - Gcs:BucketName (bắt buộc)
/// - Gcs:CredentialsPath (local dev: đường dẫn file key JSON của service account — cần để KÝ signed URL;
///   trên Cloud Run để trống, dùng service account gắn với service, cần role 'Service Account Token Creator')
/// - Gcs:MaxFileSizeMb (mặc định 10)
/// - Gcs:SignedUrlExpiryMinutes (mặc định 60, tối đa 10080 = 7 ngày theo giới hạn V4 của Google)
/// </summary>
public class GcsFileStorageService : IFileStorageService
{
    private const int MaxExpiryMinutes = 7 * 24 * 60; // V4 signed URL tối đa 7 ngày.

    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly string[] DocumentExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx"];

    private readonly string _bucketName;
    private readonly long _maxFileSizeBytes;
    private readonly int _defaultExpiryMinutes;
    private readonly Lazy<GoogleCredential> _credential;
    private readonly Lazy<StorageClient> _client;
    private readonly Lazy<UrlSigner> _signer;

    public GcsFileStorageService(IConfiguration configuration)
    {
        _bucketName = configuration["Gcs:BucketName"]
            ?? throw new ApplicationException("Missing configuration: Gcs:BucketName");
        _maxFileSizeBytes = configuration.GetValue("Gcs:MaxFileSizeMb", 10L) * 1024 * 1024;
        _defaultExpiryMinutes = Math.Clamp(
            configuration.GetValue("Gcs:SignedUrlExpiryMinutes", 60), 1, MaxExpiryMinutes);

        // Credential/client tạo lazy để app vẫn start được khi thiếu config GCS (chỉ fail lúc gọi api/files).
        var credentialsPath = configuration["Gcs:CredentialsPath"];
        _credential = new Lazy<GoogleCredential>(() =>
            string.IsNullOrWhiteSpace(credentialsPath)
                ? GoogleCredential.GetApplicationDefault() // Cloud Run / gcloud ADC
                : CredentialFactory.FromFile<ServiceAccountCredential>(credentialsPath).ToGoogleCredential());
        _client = new Lazy<StorageClient>(() => StorageClient.Create(_credential.Value));
        _signer = new Lazy<UrlSigner>(() => UrlSigner.FromCredential(_credential.Value));
    }

    public async Task<FileUploadResponse> UploadAsync(
        Stream content, string fileName, string? contentType, long sizeBytes,
        string? folder = null, bool imageOnly = false)
    {
        if (sizeBytes <= 0)
            throw new ArgumentException("File rỗng.");
        if (sizeBytes > _maxFileSizeBytes)
            throw new ArgumentException($"File vượt quá giới hạn {_maxFileSizeBytes / 1024 / 1024}MB.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowed = imageOnly ? ImageExtensions : [.. ImageExtensions, .. DocumentExtensions];
        if (!allowed.Contains(extension))
            throw new ArgumentException(
                $"Định dạng '{extension}' không được hỗ trợ. Cho phép: {string.Join(", ", allowed)}.");

        var safeFolder = string.IsNullOrWhiteSpace(folder) ? "uploads" : folder.Trim().ToLowerInvariant();
        if (!safeFolder.All(c => char.IsLetterOrDigit(c) || c is '-' or '_'))
            throw new ArgumentException("Folder chỉ được chứa chữ, số, '-' và '_'.");

        var objectName = $"{safeFolder}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

        var uploaded = await _client.Value.UploadObjectAsync(
            _bucketName,
            objectName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            content);

        var (url, expiresAt) = await SignAsync(objectName, _defaultExpiryMinutes);

        return new FileUploadResponse
        {
            ObjectName = objectName,
            Url = url,
            UrlExpiresAt = expiresAt,
            ContentType = uploaded.ContentType,
            SizeBytes = sizeBytes
        };
    }

    public async Task<SignedUrlResponse> GetSignedUrlAsync(string objectName, int? expiryMinutes = null)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("ObjectName không được rỗng.");

        var minutes = expiryMinutes ?? _defaultExpiryMinutes;
        if (minutes is < 1 or > MaxExpiryMinutes)
            throw new ArgumentException($"expiryMinutes phải trong khoảng 1–{MaxExpiryMinutes} (tối đa 7 ngày).");

        // Check tồn tại để trả 404 rõ ràng thay vì signed URL trỏ vào object không có.
        try
        {
            await _client.Value.GetObjectAsync(_bucketName, objectName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Không tìm thấy file '{objectName}' trong bucket.");
        }

        var (url, expiresAt) = await SignAsync(objectName, minutes);
        return new SignedUrlResponse { ObjectName = objectName, Url = url, UrlExpiresAt = expiresAt };
    }

    public async Task DeleteAsync(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("ObjectName không được rỗng.");

        try
        {
            await _client.Value.DeleteObjectAsync(_bucketName, objectName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Không tìm thấy file '{objectName}' trong bucket.");
        }
    }

    private async Task<(string Url, DateTime ExpiresAt)> SignAsync(string objectName, int minutes)
    {
        try
        {
            var url = await _signer.Value.SignAsync(
                _bucketName, objectName, TimeSpan.FromMinutes(minutes),
                HttpMethod.Get, SigningVersion.V4);
            return (url, DateTime.UtcNow.AddMinutes(minutes));
        }
        catch (InvalidOperationException ex)
        {
            // Credential hiện tại không có khả năng ký (ví dụ ADC bằng tài khoản cá nhân).
            throw new ApplicationException(
                "Credential hiện tại không ký được signed URL. Local dev: điền Gcs:CredentialsPath bằng file key " +
                "của service account, hoặc dùng 'gcloud auth application-default login --impersonate-service-account=<SA>'. " +
                "Cloud Run: cấp role 'Service Account Token Creator' cho service account.", ex);
        }
    }
}
