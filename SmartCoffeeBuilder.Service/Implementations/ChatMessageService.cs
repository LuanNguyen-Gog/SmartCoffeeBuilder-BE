using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using ConversationModel = SmartCoffeeBuilder.Repository.Models.Conversation;
using MessageModel = SmartCoffeeBuilder.Repository.Models.Message;
using AttachmentModel = SmartCoffeeBuilder.Repository.Models.MessageAttachment;
using ProjectWorkingModel = SmartCoffeeBuilder.Repository.Models.ProjectWorking;
using ShopOwnerModel = SmartCoffeeBuilder.Repository.Models.ShopOwner;
using ProviderModel = SmartCoffeeBuilder.Repository.Models.ServiceProviderProfile;
using AccountModel = SmartCoffeeBuilder.Repository.Models.Account;
using SmartCoffeeBuilder.Service.DTOs.Responses.Chat;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Gửi & polling tin nhắn trong thread. Phân quyền mọi method:
/// 1. Check account là member của engagement tương ứng conversation.
/// 2. Xoá message chỉ cho sender (<c>Message.SenderId == accountId</c>).
/// Sau commit, update <c>Conversation.UpdatedAt</c> để sort "thread hoạt động gần nhất"
/// trong list lấy từ <see cref="ConversationService"/> không cần polling riêng.
/// </summary>
public class ChatMessageService : IChatMessageService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IFileStorageService _fileStorage;

    public ChatMessageService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _fileStorage = fileStorage;
    }

    public async Task<List<MessageResponse>> GetSinceIdAsync(
        Guid accountId, Guid conversationId, Guid? sinceId, int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);

        var conversation = await LoadConversationAsync(conversationId);
        await EnsureMemberAsync(accountId, conversation.ProjectWorkingId);

        var repository = _unitOfWork.GetRepository<MessageModel>();

        // Id là uuid NGẪU NHIÊN (gen_random_uuid()) nên "m.Id > sinceId" KHÔNG còn nghĩa "tin mới hơn"
        // như thời id bigint tăng dần — phép so sánh vẫn chạy nhưng đúng/sai ~50/50, tức khoảng một
        // nửa tin mới sẽ không bao giờ được trả về. Phải neo theo SentAt của chính tin sinceId, và
        // chỉ dùng Id làm tiebreaker cho các tin TRÙNG mốc thời gian (khớp đúng thứ tự của orderBy).
        DateTime? anchorSentAt = null;
        if (sinceId != null)
        {
            var anchor = await repository.SingleOrDefaultAsync(
                predicate: m => m.Id == sinceId && m.ConversationId == conversationId)
                ?? throw new KeyNotFoundException("Không tìm thấy message ứng với sinceId trong thread này.");
            anchorSentAt = anchor.SentAt;
        }

        var messages = await repository.GetListAsync(
            predicate: m => m.ConversationId == conversationId
                            && (sinceId == null
                                || m.SentAt > anchorSentAt
                                || (m.SentAt == anchorSentAt && m.Id > sinceId)),
            orderBy: q => q.OrderBy(m => m.SentAt).ThenBy(m => m.Id),
            include: q => q.Include(m => m.Sender).Include(m => m.Attachments));

        var result = new List<MessageResponse>(Math.Min(limit, messages.Count));
        foreach (var m in messages.Take(limit))
        {
            var resp = MessageResponse.From(m);
            resp.Sender = await BuildSenderInfoAsync(m.Sender);
            result.Add(resp);
        }
        return result;
    }

    public async Task<List<MessageResponse>> GetSinceSentAtAsync(
        Guid accountId, Guid conversationId, DateTime? sinceSentAt, int limit = 100)
    {
        limit = Math.Clamp(limit, 1, 500);

        var conversation = await LoadConversationAsync(conversationId);
        await EnsureMemberAsync(accountId, conversation.ProjectWorkingId);

        var messages = await _unitOfWork.GetRepository<MessageModel>().GetListAsync(
            predicate: m => m.ConversationId == conversationId
                            && (sinceSentAt == null || m.SentAt > sinceSentAt),
            orderBy: q => q.OrderBy(m => m.SentAt).ThenBy(m => m.Id),
            include: q => q.Include(m => m.Sender).Include(m => m.Attachments));

        var result = new List<MessageResponse>(Math.Min(limit, messages.Count));
        foreach (var m in messages.Take(limit))
        {
            var resp = MessageResponse.From(m);
            resp.Sender = await BuildSenderInfoAsync(m.Sender);
            result.Add(resp);
        }
        return result;
    }

    /// <summary>
    /// Gửi 1 message trong thread — chỉ cần có <paramref name="body"/> HOẶC ít nhất 1 file.
    /// Không giới hạn số file đính kèm. Service ĐỌC từng stream rồi upload lên bucket —
    /// KHÔNG Dispose stream (controller giữ lifecycle).
    /// </summary>
    public async Task<MessageResponse> SendAsync(
        Guid accountId, Guid conversationId,
        string? body,
        IReadOnlyList<FilePayload>? files)
    {
        var trimmedBody = string.IsNullOrWhiteSpace(body) ? null : body.Trim();
        var validFiles = files?.Where(f => f is { SizeBytes: > 0 }).ToList();

        if (trimmedBody == null && (validFiles == null || validFiles.Count == 0))
            throw new ArgumentException("Phải có nội dung văn bản hoặc ít nhất 1 file đính kèm.");

        var conversation = await LoadConversationAsync(conversationId);
        await EnsureMemberAsync(accountId, conversation.ProjectWorkingId);

        // Upload từng file lên bucket. Nếu 1 file lỗi thì ABORT toàn bộ message — không
        // để nửa message (text OK + thiếu attachment) nếu FE quên file nào. Try/catch xoá
        // các file đã upload thành công để tránh orphan trên bucket.
        var attachments = new List<AttachmentModel>();
        var uploadedKeys = new List<string>();
        try
        {
            if (validFiles is { Count: > 0 })
            {
                var folderPath = $"{GetSenderFolderPath(accountId)}";
                foreach (var f in validFiles)
                {
                    var uploaded = await _fileStorage.UploadAsync(
                        f.Content, f.FileName, f.ContentType, f.SizeBytes, folderPath);
                    uploadedKeys.Add(uploaded.ObjectName);
                    attachments.Add(new AttachmentModel
                    {
                        Url = uploaded.ObjectName,
                        FileName = f.FileName,
                        ContentType = f.ContentType,
                        SizeBytes = f.SizeBytes,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            var message = new MessageModel
            {
                ConversationId = conversation.Id,
                SenderId = accountId,
                Body = trimmedBody,
                SentAt = DateTime.UtcNow,
                Attachments = attachments
            };
            await _unitOfWork.GetRepository<MessageModel>().InsertAsync(message);
            await _unitOfWork.CommitAsync();

            await TouchConversationAsync(conversation.Id);

            var reloaded = await LoadMessageAsync(message.Id);
            var response = MessageResponse.From(reloaded);
            response.Sender = await BuildSenderInfoAsync(reloaded.Sender);
            return response;
        }
        catch
        {
            // Dọn orphan trên bucket — best-effort, không chặn lỗi gốc.
            foreach (var key in uploadedKeys)
                await _fileStorage.TryDeleteAsync(key);
            throw;
        }
    }

    /// <summary>Trả folder "{role}/{accountId}" để upload đúng quy ước của api/files.</summary>
    private async Task<string> GetSenderFolderPath(Guid accountId)
    {
        var sender = await _unitOfWork.GetRepository<AccountModel>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId)
            ?? throw new KeyNotFoundException($"Không tìm thấy account id {accountId}.");
        return $"{sender.Role}/{sender.Id}";
    }

    public async Task DeleteAsync(Guid accountId, Guid messageId)
    {
        var message = await _unitOfWork.GetRepository<MessageModel>().SingleOrDefaultAsync(
            predicate: m => m.Id == messageId)
            ?? throw new KeyNotFoundException($"Không tìm thấy message với id {messageId}.");

        // Phân quyền: chỉ sender mới xoá được message.
        if (message.SenderId != accountId)
            throw new UnauthorizedAccessException(
                "Chỉ người gửi mới có quyền xoá tin nhắn này.");

        var conversationId = message.ConversationId;

        // Xoá attachment trên bucket sau khi commit DB.
        var attachments = await _unitOfWork.GetRepository<AttachmentModel>().GetListAsync(
            predicate: a => a.MessageId == messageId);

        _unitOfWork.GetRepository<MessageModel>().Delete(message);
        await _unitOfWork.CommitAsync();

        // Dọn file trên bucket — bỏ qua nếu lỗi (best-effort).
        foreach (var att in attachments)
            await _fileStorage.TryDeleteAsync(att.Url);

        // Có thể đã xoá xong thread → touch idempotent (TryUpdateConversationTimestamp nếu conversation còn).
        await TouchConversationAsync(conversationId);
    }

    // ───────── Helpers ─────────

    private async Task<ConversationModel> LoadConversationAsync(Guid conversationId)
    {
        return await _unitOfWork.GetRepository<ConversationModel>().SingleOrDefaultAsync(
            predicate: c => c.Id == conversationId)
            ?? throw new KeyNotFoundException($"Không tìm thấy conversation với id {conversationId}.");
    }

    private async Task<MessageModel> LoadMessageAsync(Guid messageId)
    {
        return await _unitOfWork.GetRepository<MessageModel>().SingleOrDefaultAsync(
            predicate: m => m.Id == messageId,
            include: q => q.Include(m => m.Sender).Include(m => m.Attachments))
            ?? throw new KeyNotFoundException($"Không tìm thấy message với id {messageId}.");
    }

    private async Task EnsureMemberAsync(Guid accountId, Guid projectWorkingId)
    {
        var pw = await _unitOfWork.GetRepository<ProjectWorkingModel>().SingleOrDefaultAsync(
            predicate: p => p.Id == projectWorkingId,
            include: q => q.Include(p => p.ProjectShopOwner).ThenInclude(s => s.Owner))
            ?? throw new KeyNotFoundException($"Không tìm thấy engagement với id {projectWorkingId}.");

        if (pw.ProjectShopOwner.Owner.AccountId == accountId)
            return;

        var otherPws = await _unitOfWork.GetRepository<ProjectWorkingModel>().GetListAsync(
            predicate: p => p.ProjectShopOwnerId == pw.ProjectShopOwnerId
                            && p.ServiceProviderProfile.AccountId == accountId
                            && p.Status == ProviderStatus.accepted);
        if (!otherPws.Any())
            throw new UnauthorizedAccessException(
                "Account không thuộc engagement này — không có quyền gửi/xem tin nhắn.");
    }

    private async Task<SenderInfo> BuildSenderInfoAsync(AccountModel account)
        => await SenderInfoFactory.BuildAsync(
            account,
            _unitOfWork.GetRepository<ShopOwnerModel>(),
            _unitOfWork.GetRepository<ProviderModel>());

    /// <summary>Cập nhật <c>Conversation.UpdatedAt = now</c> để sort list theo hoạt động.</summary>
    private async Task TouchConversationAsync(Guid conversationId)
    {
        // Best-effort: nếu conversation đã bị xoá thì bỏ qua.
        var conv = await _unitOfWork.GetRepository<ConversationModel>().GetByIdAsync(conversationId);
        if (conv == null) return;

        conv.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.GetRepository<ConversationModel>().Update(conv);
        await _unitOfWork.CommitAsync();
    }
}
