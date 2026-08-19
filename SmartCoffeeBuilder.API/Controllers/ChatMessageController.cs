using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.API.Controllers;

/// <summary>
/// Tin nhắn trong thread (Conversation). Gửi và polling.
/// </summary>
[ApiController]
[Route("api/chat/messages")]
[Authorize]
public class ChatMessageController : ControllerBase
{
    private readonly IChatMessageService _service;

    public ChatMessageController(IChatMessageService service)
    {
        _service = service;
    }

    /// <summary>
    /// Polling message mới trong 1 thread. Nếu <c>sinceId</c> có giá trị thì dùng (service tra
    /// <c>SentAt</c> của message đó rồi lấy các message gửi sau); nếu không thì fallback
    /// <c>sinceSentAt</c>; nếu cả hai rỗng thì trả về message đầu tiên theo SentAt ASC
    /// (giới hạn <paramref name="limit"/>).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Poll(
        [FromQuery] Guid conversationId,
        [FromQuery] Guid? sinceId = null,
        [FromQuery] DateTime? sinceSentAt = null,
        [FromQuery] int limit = 100)
    {
        var accountId = User.GetAccountId();
        var result = sinceId != null
            ? await _service.GetSinceIdAsync(accountId, conversationId, sinceId, limit)
            : await _service.GetSinceSentAtAsync(accountId, conversationId, sinceSentAt, limit);
        return Ok(result);
    }

    /// <summary>
    /// Gửi message — multipart form-data, KHÔNG giới hạn số file đính kèm.
    /// Chỉ cần có <c>body</c> (text) HOẶC ít nhất 1 file. Body + file gửi cùng nhau cũng OK.
    /// Kích thước body tối đa đặt bằng <c>Program.cs</c> (<c>Math.Max(Gcs:MaxFileSizeMb, 50) + 1 MB</c>).
    /// </summary>
    /// <remarks>
    /// multipart/form-data:
    /// <list type="bullet">
    /// <item>body (string, optional)</item>
    /// <item>files (IFormFileCollection, optional, multi)</item>
    /// </list>
    /// </remarks>
    [HttpPost("{conversationId:guid}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Send(
        Guid conversationId,
        [FromForm] string? body = null,
        IFormFileCollection? files = null)
    {
        var payloads = BuildPayloads(files);

        var result = await _service.SendAsync(
            User.GetAccountId(),
            conversationId,
            body,
            payloads.Count == 0 ? null : payloads);

        return Ok(result);
    }

    /// <summary>Xoá message — chỉ sender. Cascade xoá file đính kèm (cả DB lẫn bucket).</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(User.GetAccountId(), id);
        return NoContent();
    }

    /// <summary>
    /// Mở stream từ <see cref="IFormFile"/> cho từng file hợp lệ (bỏ qua file rỗng).
    /// Controller Dispose streams qua <see cref="IFormFile"/> lifecycle (ASP.NET tự giải phóng
    /// sau khi action hoàn tất), service không Dispose.
    /// </summary>
    private static List<FilePayload> BuildPayloads(IFormFileCollection? files)
    {
        if (files == null || files.Count == 0) return new List<FilePayload>();

        var result = new List<FilePayload>(files.Count);
        foreach (var f in files)
        {
            if (f.Length <= 0) continue;
            result.Add(new FilePayload(
                Content: f.OpenReadStream(),
                FileName: f.FileName,
                ContentType: f.ContentType,
                SizeBytes: f.Length));
        }
        return result;
    }
}