using SmartCoffeeBuilder.Service.DTOs.Responses.Chat;

namespace SmartCoffeeBuilder.Service.Interfaces;

/// <summary>
/// Quản lý tin nhắn trong thread (<c>Conversation</c>) và file đính kèm.
/// Mọi method phải check account là "member" của engagement tương ứng với conversation.
/// Real-time sử dụng polling — FE gọi lại <see cref="GetSinceIdAsync"/> sau một khoảng.
/// </summary>
public interface IChatMessageService
{
    /// <summary>
    /// Polling: lấy mọi message GỬI SAU message <paramref name="sinceId"/> trong 1 thread
    /// (<c>SentAt ASC</c>, limit mặc định 100). sinceId null = lấy tất cả (lần đầu mở thread).
    /// <para>
    /// Mốc so sánh là <c>SentAt</c> của chính message sinceId, KHÔNG phải <c>Id &gt; sinceId</c>:
    /// Id là uuid ngẫu nhiên nên so sánh thứ tự trên nó không phản ánh thời điểm gửi.
    /// </para>
    /// </summary>
    /// <exception cref="KeyNotFoundException">sinceId không thuộc thread này (HTTP 404) — FE nên resync.</exception>
    Task<List<MessageResponse>> GetSinceIdAsync(
        Guid accountId, Guid conversationId, Guid? sinceId, int limit = 100);

    /// <summary>
    /// Polling theo mốc thời gian — dùng khi FE không có <c>sinceId</c> (reconnect, refresh tab).
    /// Lấy message có <c>SentAt &gt; sinceSentAt</c>.
    /// </summary>
    Task<List<MessageResponse>> GetSinceSentAtAsync(
        Guid accountId, Guid conversationId, DateTime? sinceSentAt, int limit = 100);

    /// <summary>
    /// Gửi 1 message trong thread — chỉ cần có <paramref name="body"/> hoặc ít nhất 1 file.
    /// Không giới hạn số file đính kèm. Stream + metadata của từng file do controller truyền
    /// xuống (mở từ <c>IFormFile</c> rồi Dispose); service chỉ xử lý <see cref="System.IO.Stream"/>
    /// để không phụ thuộc ASP.NET Core. Trả về message vừa lưu kèm URL public các file.
    /// </summary>
    /// <exception cref="ArgumentException">Cả body và danh sách file đều rỗng (HTTP 400).</exception>
    Task<MessageResponse> SendAsync(
        Guid accountId, Guid conversationId,
        string? body,
        IReadOnlyList<FilePayload>? files);

    /// <summary>Xoá message — chỉ sender (giống Discord). Ảnh hưởng cascade tới attachment.</summary>
    Task DeleteAsync(Guid accountId, Guid messageId);
}

/// <summary>
/// Một file đính kèm trong request multipart. Controller build từ <c>IFormFile</c>
/// rồi truyền xuống service — service KHÔNG đọc trực tiếp từ HTTP.
/// </summary>
/// <param name="Content">Stream phải mở sẵn. Service ĐỌC hết rồi KHÔNG Dispose (controller giữ lifecycle).</param>
/// <param name="FileName">Tên file gốc — service lưu làm metadata.</param>
/// <param name="ContentType">MIME type từ form-data.</param>
/// <param name="SizeBytes">Kích thước file để validate và lưu metadata.</param>
public record FilePayload(
    Stream Content,
    string FileName,
    string? ContentType,
    long SizeBytes);