using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using SmartCoffeeBuilder.Service.DTOs.Responses.File;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Upload/xoá/đọc file trên Google Cloud Storage — bucket PRIVATE (Public access prevention bật).
/// Không dùng signed URL: file được BE stream trực tiếp qua GET api/files/view (URL cố định,
/// không hết hạn); DB lưu objectName. Chỉ cần quyền Storage Object Admin, không cần quyền ký.
///
/// Config (appsettings):
/// - Gcs:BucketName (bắt buộc)
/// - Gcs:CredentialsPath (local dev: file key JSON nếu có; để trống dùng ADC — Cloud Run/gcloud login)
/// - Gcs:MaxFileSizeMb (mặc định 10)
/// </summary>
public class GcsFileStorageService : IFileStorageService
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly string[] DocumentExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx"];

    private readonly string _bucketName;
    private readonly long _maxFileSizeBytes;
    private readonly Lazy<StorageClient> _client;

    public GcsFileStorageService(IConfiguration configuration)
    {
        _bucketName = configuration["Gcs:BucketName"]
            ?? throw new ApplicationException("Missing configuration: Gcs:BucketName");
        _maxFileSizeBytes = configuration.GetValue("Gcs:MaxFileSizeMb", 10L) * 1024 * 1024;

        // Client tạo lazy để app vẫn start được khi thiếu config GCS (chỉ fail lúc gọi api/files).
        var credentialsPath = configuration["Gcs:CredentialsPath"];
        _client = new Lazy<StorageClient>(() =>
            string.IsNullOrWhiteSpace(credentialsPath)
                ? StorageClient.Create() // Application Default Credentials (Cloud Run / gcloud ADC)
                : StorageClient.Create(
                    CredentialFactory.FromFile<ServiceAccountCredential>(credentialsPath).ToGoogleCredential()));
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

        return new FileUploadResponse
        {
            ObjectName = objectName,
            // Đường dẫn tương đối trên chính BE — FE ghép base URL của API vào trước.
            Url = $"/api/files/view?objectName={Uri.EscapeDataString(objectName)}",
            ContentType = uploaded.ContentType,
            SizeBytes = sizeBytes
        };
    }

    public async Task<FileDownloadResult> DownloadAsync(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            throw new ArgumentException("ObjectName không được rỗng.");

        try
        {
            var metadata = await _client.Value.GetObjectAsync(_bucketName, objectName);

            var stream = new MemoryStream();
            await _client.Value.DownloadObjectAsync(_bucketName, objectName, stream);
            stream.Position = 0;

            return new FileDownloadResult
            {
                Content = stream,
                ContentType = string.IsNullOrWhiteSpace(metadata.ContentType)
                    ? "application/octet-stream"
                    : metadata.ContentType,
                FileName = Path.GetFileName(objectName)
            };
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException($"Không tìm thấy file '{objectName}' trong bucket.");
        }
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
}
