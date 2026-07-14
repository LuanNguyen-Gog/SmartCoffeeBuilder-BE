using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using SmartCoffeeBuilder.Service.DTOs.Responses.File;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Upload/xoá/đọc file trên Google Cloud Storage — bucket PUBLIC-READ: FE dùng thẳng URL
/// https://storage.googleapis.com/{bucket}/{object} (xem được ẩn danh, không hết hạn, cache qua CDN).
/// DB lưu objectName; GET api/files/view vẫn giữ để tương thích dữ liệu cũ.
///
/// Yêu cầu cấu hình bucket (một lần, bằng gcloud):
///   gcloud storage buckets update gs://{bucket} --no-public-access-prevention
///   gcloud storage buckets add-iam-policy-binding gs://{bucket} --member=allUsers --role=roles/storage.objectViewer
///
/// Config (appsettings):
/// - Gcs:BucketName (bắt buộc)
/// - Gcs:PublicBaseUrl (tuỳ chọn — mặc định "https://storage.googleapis.com/{BucketName}";
///   đổi khi gắn CDN/custom domain trước bucket)
/// - Gcs:CredentialsPath (local dev: file key JSON nếu có; để trống dùng ADC — Cloud Run/gcloud login)
/// - Gcs:MaxFileSizeMb (mặc định 10)
/// </summary>
public class GcsFileStorageService : IFileStorageService
{
    private static readonly string[] ImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    private static readonly string[] DocumentExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx"];

    private readonly string _bucketName;
    private readonly string _publicBaseUrl;
    private readonly long _maxFileSizeBytes;
    private readonly Lazy<StorageClient> _client;

    public GcsFileStorageService(IConfiguration configuration)
    {
        _bucketName = configuration["Gcs:BucketName"]
            ?? throw new ApplicationException("Missing configuration: Gcs:BucketName");
        _publicBaseUrl = (configuration["Gcs:PublicBaseUrl"]
            ?? $"https://storage.googleapis.com/{_bucketName}").TrimEnd('/');
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
        string folderPath, bool imageOnly = false)
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

        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("FolderPath không được rỗng.");
        var safeFolder = folderPath.Trim().Trim('/').ToLowerInvariant();
        if (!safeFolder.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '/'))
            throw new ArgumentException("FolderPath chỉ được chứa chữ, số, '-', '_' và '/'.");

        var objectName = $"{safeFolder}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";

        var uploaded = await _client.Value.UploadObjectAsync(
            _bucketName,
            objectName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            content);

        return new FileUploadResponse
        {
            ObjectName = objectName,
            // URL public tuyệt đối — FE dùng thẳng (img src, mở tab ẩn danh đều xem được).
            Url = $"{_publicBaseUrl}/{objectName}",
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
