using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using ConversationModel = SmartCoffeeBuilder.Repository.Models.Conversation;
using MessageModel = SmartCoffeeBuilder.Repository.Models.Message;
using ProjectWorkingModel = SmartCoffeeBuilder.Repository.Models.ProjectWorking;
using ShopOwnerModel = SmartCoffeeBuilder.Repository.Models.ShopOwner;
using ProviderModel = SmartCoffeeBuilder.Repository.Models.ServiceProviderProfile;
using AccountModel = SmartCoffeeBuilder.Repository.Models.Account;
using ProjectShopOwnerModel = SmartCoffeeBuilder.Repository.Models.ProjectShopOwner;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Chat;
using SmartCoffeeBuilder.Service.DTOs.Responses.Chat;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// CRUD thread chat trong engagement. Quy tắc:
/// - List/Get/Create/Update: cả owner và provider (member) đều được
/// - Delete: chỉ người tạo thread (CreatedBy == accountId) → tránh xoá nhầm
/// - Topic rỗng: service sinh "Thread #N" của engagement (count + 1)
/// </summary>
public class ConversationService : IConversationService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;

    public ConversationService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PaginationResponse<ConversationSummary>> GetByEngagementAsync(
        Guid accountId, Guid projectWorkingId, int pageNumber = 1, int pageSize = 20)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var engagement = await EnsureMemberAsync(accountId, projectWorkingId);

        // Lấy tất cả engagement (ProjectWorking) của cùng project để cả owner + các provider accepted
        // đều thấy thread của nhau — thread gắn với project, không gắn với từng engagement.
        var pwRepo = _unitOfWork.GetRepository<ProjectWorkingModel>();
        var allEngagementIds = (await pwRepo.GetListAsync(
            predicate: p => p.ProjectShopOwnerId == engagement.ProjectShopOwnerId))
            .Select(p => p.Id)
            .ToList();

        var conversations = await _unitOfWork.GetRepository<ConversationModel>().GetListAsync(
            predicate: c => allEngagementIds.Contains(c.ProjectWorkingId),
            include: q => q.Include(c => c.CreatedByAccount));

        // Sort theo UpdatedAt DESC; các thread cùng UpdatedAt ổn định theo CreatedAt DESC.
        var sorted = conversations
            .OrderByDescending(c => c.UpdatedAt)
            .ThenByDescending(c => c.CreatedAt)
            .ToList();

        // Bỏ qua page -1 nếu pagination xảy ra ở client; còn cách dưới đây vẫn hỗ trợ.
        var paged = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var summaries = await BuildSummariesAsync(paged);
        return new PaginationResponse<ConversationSummary>(
            summaries, sorted.Count, pageNumber, pageSize);
    }

    public async Task<ConversationDetailResponse> GetByIdAsync(
        Guid accountId, Guid conversationId, int pageNumber = 1, int pageSize = 50)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var conversation = await LoadConversationAsync(conversationId);

        // Phân quyền: phải là member của engagement mới xem được.
        await EnsureMemberAsync(accountId, conversation.ProjectWorkingId);

        // Load message theo phân trang + sort ASC (FE hiển thị cronological).
        var messages = await _unitOfWork.GetRepository<MessageModel>().GetListAsync(
            predicate: m => m.ConversationId == conversationId,
            orderBy: q => q.OrderBy(m => m.SentAt).ThenBy(m => m.Id));

        var sorted = messages.ToList();
        var total = sorted.Count;
        var sliced = sorted
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var detail = ConversationDetailResponse.From(conversation, sliced);
        detail.CreatedBy = await BuildSenderInfoAsync(conversation.CreatedByAccount);
        for (var i = 0; i < sliced.Count; i++)
        {
            detail.Messages[i].Sender = await BuildSenderInfoAsync(sliced[i].Sender);
        }

        // Để controller dùng thêm nếu cần tổng số tin — set về ConversationDetailResponse.Extensions? Skip cho v1.
        return detail;
    }

    public async Task<ConversationSummary> CreateAsync(Guid accountId, CreateConversationRequest request)
    {
        var engagement = await EnsureMemberAsync(accountId, request.ProjectWorkingId);

        // Đếm tất cả thread của project (mọi engagement) để auto-name "Thread #N" không trùng.
        var pwRepo = _unitOfWork.GetRepository<ProjectWorkingModel>();
        var allEngagementIds = (await pwRepo.GetListAsync(
            predicate: p => p.ProjectShopOwnerId == engagement.ProjectShopOwnerId))
            .Select(p => p.Id)
            .ToList();

        var existingCount = await _unitOfWork.GetRepository<ConversationModel>()
            .CountAsync(c => allEngagementIds.Contains(c.ProjectWorkingId));

        var topic = string.IsNullOrWhiteSpace(request.Topic)
            ? $"Thread #{existingCount + 1}"
            : request.Topic.Trim();

        var now = DateTime.UtcNow;
        var conversation = new ConversationModel
        {
            ProjectWorkingId = engagement.Id,
            Topic = topic,
            CreatedBy = accountId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.GetRepository<ConversationModel>().InsertAsync(conversation);
        await _unitOfWork.CommitAsync();

        // Load lại để có navigation cho From().
        var reloaded = await LoadConversationAsync(conversation.Id);
        var list = new List<ConversationModel> { reloaded };
        var result = (await BuildSummariesAsync(list)).First();
        return result;
    }

    public async Task<ConversationSummary> UpdateAsync(Guid accountId, Guid conversationId, UpdateConversationRequest request)
    {
        var conversation = await LoadConversationAsync(conversationId);
        await EnsureMemberAsync(accountId, conversation.ProjectWorkingId);

        if (request.Topic != null)
        {
            var trimmed = request.Topic.Trim();
            // Trim rỗng → đặt "Thread" mặc định (tránh để trắng do nhập dấu cách).
            conversation.Topic = trimmed.Length == 0 ? "Thread" : trimmed;
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.GetRepository<ConversationModel>().Update(conversation);
        await _unitOfWork.CommitAsync();

        var reloaded = await LoadConversationAsync(conversationId);
        var list = new List<ConversationModel> { reloaded };
        var result = (await BuildSummariesAsync(list)).First();
        return result;
    }

    public async Task DeleteAsync(Guid accountId, Guid conversationId)
    {
        var conversation = await LoadConversationAsync(conversationId);

        // Phân quyền: chỉ creator mới xoá được thread (tránh xoá nhầm thread người khác tạo).
        if (conversation.CreatedBy != accountId)
            throw new UnauthorizedAccessException(
                "Chỉ người tạo thread mới có quyền xoá — thành viên khác không thể xoá thread của bạn.");

        _unitOfWork.GetRepository<ConversationModel>().Delete(conversation);
        await _unitOfWork.CommitAsync();
    }

    // ───────── Helpers ─────────

    /// <summary>
    /// Load 1 conversation + CreatedByAccount navigation.
    /// </summary>
    private async Task<ConversationModel> LoadConversationAsync(Guid conversationId)
    {
        return await _unitOfWork.GetRepository<ConversationModel>().SingleOrDefaultAsync(
            predicate: c => c.Id == conversationId,
            include: q => q.Include(c => c.CreatedByAccount))
            ?? throw new KeyNotFoundException($"Không tìm thấy conversation với id {conversationId}.");
    }

    /// <summary>
    /// Check account phải là member (owner hoặc provider) của engagement. Throw nếu sai.
    /// Dùng chung cho cả ConversationService và ChatMessageService — copy code thay vì tách
    /// utility vì chỉ hai chỗ dùng, không abstract quá sớm.
    /// </summary>
    private async Task<ProjectWorkingModel> EnsureMemberAsync(Guid accountId, Guid projectWorkingId)
    {
        // Load engagement để trả về thông tin project.
        var pw = await _unitOfWork.GetRepository<ProjectWorkingModel>().SingleOrDefaultAsync(
            predicate: p => p.Id == projectWorkingId,
            include: q => q.Include(p => p.ProjectShopOwner).ThenInclude(s => s.Owner))
            ?? throw new KeyNotFoundException($"Không tìm thấy engagement với id {projectWorkingId}.");

        // Owner: luôn là member.
        if (pw.ProjectShopOwner.Owner.AccountId == accountId)
            return pw;

        // Provider: phải có ProjectWorking accepted cho cùng project này và accountId khớp.
        var otherPws = await _unitOfWork.GetRepository<ProjectWorkingModel>().GetListAsync(
            predicate: p => p.ProjectShopOwnerId == pw.ProjectShopOwnerId
                            && p.ServiceProviderProfile.AccountId == accountId
                            && p.Status == ProviderStatus.accepted);
        if (!otherPws.Any())
            throw new UnauthorizedAccessException(
                "Account không thuộc engagement này — không có quyền truy cập thread chat.");

        return pw;
    }

    /// <summary>
    /// Build SenderInfo async từ Account; dùng chung cho CreatedBy và các sender trong MessageResponse.
    /// Resolve <c>Account</c> qua EF projection để tránh load cả ProjectShopOwner navigation.
    /// </summary>
    private async Task<SenderInfo> BuildSenderInfoAsync(AccountModel account)
    {
        return await SenderInfoFactory.BuildAsync(
            account,
            _unitOfWork.GetRepository<ShopOwnerModel>(),
            _unitOfWork.GetRepository<ProviderModel>());
    }

    /// <summary>
    /// Map nhiều Conversation → ConversationSummary với LastMessage và CreatedBy
    /// (batch-load message mới nhất qua GroupBy, đỡ N+1 query).
    /// </summary>
    private async Task<List<ConversationSummary>> BuildSummariesAsync(IReadOnlyList<ConversationModel> conversations)
    {
        if (conversations.Count == 0) return new List<ConversationSummary>();

        var convIds = conversations.Select(c => c.Id).ToList();

        // Batch: lấy message cuối của từng conversation (1 SQL có GROUP BY).
        var messageRepo = _unitOfWork.GetRepository<MessageModel>();
        var lastMessages = await messageRepo.GetListAsync(
            predicate: m => convIds.Contains(m.ConversationId),
            orderBy: q => q.OrderByDescending(m => m.SentAt).ThenByDescending(m => m.Id),
            include: q => q.Include(m => m.Sender).Include(m => m.Attachments));
        var groupedLast = lastMessages
            .GroupBy(m => m.ConversationId)
            .ToDictionary(g => g.Key, g => g.First());

        // Build sender info cho CreatedBy (load account nếu cần — đã có Conversation.CreatedByAccount).
        var result = new List<ConversationSummary>(conversations.Count);
        foreach (var c in conversations)
        {
            var summary = ConversationSummary.From(c);
            summary.CreatedBy = await BuildSenderInfoAsync(c.CreatedByAccount);
            if (groupedLast.TryGetValue(c.Id, out var last))
            {
                var lastResp = MessageResponse.From(last);
                lastResp.Sender = await BuildSenderInfoAsync(last.Sender);
                summary.LastMessage = lastResp;
            }
            result.Add(summary);
        }
        return result;
    }
}
