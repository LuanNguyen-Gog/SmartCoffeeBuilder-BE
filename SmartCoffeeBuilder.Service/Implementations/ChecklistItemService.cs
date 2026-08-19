using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Checklist;
using SmartCoffeeBuilder.Service.DTOs.Responses.Checklist;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Checklist nghiệm thu cho bản thiết kế và hạng mục thi công (review 3, 17/08/2026).
///
/// Phân vai rõ: PROVIDER dựng danh sách mục cần nghiệm thu, OWNER chấm đạt / không đạt kèm minh
/// chứng và ghi chú "cần sửa gì". Không gộp hai quyền này làm một — chính điểm hội đồng góp ý là
/// nghiệm thu phải có bên thứ hai xác nhận, chứ không phải provider tự khai đã xong.
/// </summary>
public class ChecklistItemService : IChecklistItemService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ChecklistItem> _repository;
    private readonly IFileStorageService _fileStorage;

    public ChecklistItemService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ChecklistItem>();
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<ChecklistItemResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 50,
        Guid? designId = null, Guid? constructionItemId = null, string? status = null)
    {
        var st = ParseStatus(status);
        var isAdmin = await IsAdminAsync(accountId);

        var query = _repository
            .GetQueryable(
                c => (designId == null || c.DesignId == designId)
                     && (constructionItemId == null || c.ConstructionItemId == constructionItemId)
                     && (st == null || c.Status == st)
                     && (isAdmin
                         || (c.Design != null
                             && (c.Design.ProjectWorking.ProjectShopOwner.Owner.AccountId == accountId
                                 || c.Design.ProjectWorking.ServiceProviderProfile.AccountId == accountId))
                         || (c.ConstructionItem != null
                             && (c.ConstructionItem.ProjectWorking.ProjectShopOwner.Owner.AccountId == accountId
                                 || c.ConstructionItem.ProjectWorking.ServiceProviderProfile.AccountId == accountId))),
                include: BuildInclude())
            .OrderBy(c => c.SortOrder).ThenBy(c => c.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ChecklistItemResponse>(
            paged.Items.Select(ChecklistItemResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ChecklistItemResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var item = await LoadAsync(id);
        await ResolveActorAsync(accountId, item);
        return ChecklistItemResponse.From(item);
    }

    /// <summary>
    /// Provider lập một hoặc nhiều mục nghiệm thu cùng lúc (checklist thường nhập theo cụm).
    /// </summary>
    public async Task<List<ChecklistItemResponse>> CreateAsync(Guid accountId, CreateChecklistItemsRequest request)
    {
        if ((request.DesignId == null) == (request.ConstructionItemId == null))
            throw new ArgumentException(
                "Phải gửi ĐÚNG MỘT trong hai: designId (nghiệm thu bản thiết kế) hoặc constructionItemId (nghiệm thu hạng mục thi công).");

        if (request.Items.Count == 0)
            throw new ArgumentException("Phải có ít nhất một mục nghiệm thu.");

        var projectWorkingId = await ResolveTargetEngagementAsync(request.DesignId, request.ConstructionItemId);
        EnsureActor(
            await ResolveActorByEngagementAsync(accountId, projectWorkingId),
            "lập checklist nghiệm thu", ChecklistActor.Provider);

        // Nối tiếp thứ tự đang có thay vì đánh lại từ 0 — nhập checklist thành nhiều đợt là chuyện
        // bình thường, đánh lại từ 0 sẽ làm hai cụm chồng số lên nhau.
        var existing = await _repository.GetListAsync(
            selector: c => c.SortOrder,
            predicate: c => (request.DesignId != null && c.DesignId == request.DesignId)
                            || (request.ConstructionItemId != null && c.ConstructionItemId == request.ConstructionItemId));
        var sortOrder = existing.Count == 0 ? 0 : existing.Max() + 1;

        var now = DateTime.UtcNow;
        var items = request.Items.Select(i =>
        {
            if (string.IsNullOrWhiteSpace(i.Name))
                throw new ArgumentException("Mỗi mục nghiệm thu phải có tên.");

            return new ChecklistItem
            {
                DesignId = request.DesignId,
                ConstructionItemId = request.ConstructionItemId,
                Name = i.Name,
                Description = i.Description,
                IsRequired = i.IsRequired ?? true,
                SortOrder = sortOrder++,
                Status = ChecklistStatus.pending,
                CreatedBy = accountId,
                CreatedAt = now,
                UpdatedAt = now
            };
        }).ToList();

        await _repository.InsertRangeAsync(items);
        await _unitOfWork.CommitAsync();

        return items.Select(ChecklistItemResponse.From).ToList();
    }

    public async Task<ChecklistItemResponse> UpdateAsync(Guid accountId, Guid id, UpdateChecklistItemRequest request)
    {
        var item = await LoadAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, item), "sửa mục nghiệm thu", ChecklistActor.Provider);

        if (request.Name != null) item.Name = request.Name;
        if (request.Description != null) item.Description = request.Description;
        if (request.IsRequired.HasValue) item.IsRequired = request.IsRequired.Value;
        if (request.SortOrder.HasValue) item.SortOrder = request.SortOrder.Value;

        item.UpdatedAt = DateTime.UtcNow;
        _repository.Update(item);
        await _unitOfWork.CommitAsync();

        return ChecklistItemResponse.From(item);
    }

    /// <summary>
    /// Owner chấm một mục: đạt / chưa đạt, kèm minh chứng và ghi chú cần sửa gì.
    /// Chấm lại được nhiều lần — provider sửa xong thì owner đổi 'failed' thành 'passed'.
    /// </summary>
    public async Task<ChecklistItemResponse> CheckAsync(Guid accountId, Guid id, CheckChecklistItemRequest request)
    {
        var item = await LoadAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, item), "chấm nghiệm thu", ChecklistActor.Owner);

        if (!Enum.TryParse<ChecklistStatus>(request.Status?.Trim(), ignoreCase: true, out var status)
            || status == ChecklistStatus.pending)
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: passed, failed.");

        // Chấm 'failed' mà không nói vì sao thì provider không biết đường sửa — đúng ý review 3
        // ("cái nào chưa đạt hay cần sửa cái gì").
        if (status == ChecklistStatus.failed && string.IsNullOrWhiteSpace(request.Note))
            throw new ArgumentException("Chấm 'failed' thì phải ghi chú rõ chưa đạt ở chỗ nào / cần sửa gì.");

        var now = DateTime.UtcNow;
        item.Status = status;
        item.Note = request.Note;
        item.EvidenceUrl = await _fileStorage.NormalizeForStorageAsync(request.EvidenceUrl, "evidenceUrl")
                           ?? item.EvidenceUrl;
        item.CheckedBy = accountId;
        item.CheckedAt = now;
        item.UpdatedAt = now;

        _repository.Update(item);
        await _unitOfWork.CommitAsync();

        return ChecklistItemResponse.From(item);
    }

    /// <summary>
    /// Provider đính minh chứng cho một mục (ảnh hiện trường, biên bản…) mà không chấm điểm —
    /// chấm vẫn là việc của owner.
    /// </summary>
    public async Task<ChecklistItemResponse> AttachEvidenceAsync(
        Guid accountId, Guid id, AttachChecklistEvidenceRequest request)
    {
        var item = await LoadAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, item), "đính minh chứng nghiệm thu", ChecklistActor.Provider);

        item.EvidenceUrl = await _fileStorage.NormalizeForStorageAsync(request.EvidenceUrl, "evidenceUrl")
            ?? throw new ArgumentException("evidenceUrl không được để trống.");
        item.UpdatedAt = DateTime.UtcNow;

        _repository.Update(item);
        await _unitOfWork.CommitAsync();

        return ChecklistItemResponse.From(item);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var item = await LoadAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, item), "xoá mục nghiệm thu", ChecklistActor.Provider);

        if (item.Status != ChecklistStatus.pending)
            throw new InvalidOperationException(
                $"Mục đã được chấm '{item.Status}' — không xoá được (giữ lại làm vết nghiệm thu).");

        var evidence = item.EvidenceUrl;

        _repository.Delete(item);
        await _unitOfWork.CommitAsync();

        await _fileStorage.TryDeleteAsync(evidence);
    }

    // ───────────────────────── Quyền ─────────────────────────

    private enum ChecklistActor { Owner, Provider, Admin }

    private sealed record EngagementParties(Guid OwnerAccountId, Guid ProviderAccountId);

    /// <summary>Quyền suy ra từ engagement của hạng mục cha (design hoặc construction_item).</summary>
    private async Task<ChecklistActor> ResolveActorAsync(Guid accountId, ChecklistItem item)
    {
        var projectWorkingId = await ResolveTargetEngagementAsync(item.DesignId, item.ConstructionItemId);
        return await ResolveActorByEngagementAsync(accountId, projectWorkingId);
    }

    private async Task<ChecklistActor> ResolveActorByEngagementAsync(Guid accountId, Guid projectWorkingId)
    {
        var parties = (await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
                selector: e => new EngagementParties(
                    e.ProjectShopOwner.Owner.AccountId,
                    e.ServiceProviderProfile.AccountId),
                predicate: e => e.Id == projectWorkingId))
            .FirstOrDefault()
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {projectWorkingId}.");

        if (parties.OwnerAccountId == accountId) return ChecklistActor.Owner;
        if (parties.ProviderAccountId == accountId) return ChecklistActor.Provider;
        if (await IsAdminAsync(accountId)) return ChecklistActor.Admin;

        throw new UnauthorizedAccessException(
            "Checklist này thuộc về một hợp tác mà tài khoản đang đăng nhập không tham gia.");
    }

    private static void EnsureActor(ChecklistActor actual, string action, params ChecklistActor[] allowed)
    {
        if (actual == ChecklistActor.Admin || allowed.Contains(actual)) return;

        var who = string.Join(" hoặc ", allowed.Select(
            a => a == ChecklistActor.Owner ? "chủ quán" : "nhà cung cấp"));
        throw new UnauthorizedAccessException($"Chỉ {who} của hợp tác này mới được {action}.");
    }

    private async Task<Guid> ResolveTargetEngagementAsync(Guid? designId, Guid? constructionItemId)
    {
        if (designId != null)
        {
            var design = await _unitOfWork.GetRepository<Design>()
                .SingleOrDefaultAsync(selector: d => d.ProjectWorkingId, predicate: d => d.Id == designId);
            if (design == Guid.Empty)
                throw new KeyNotFoundException($"Không tìm thấy design với id {designId}.");
            return design;
        }

        var item = await _unitOfWork.GetRepository<ConstructionItem>()
            .SingleOrDefaultAsync(selector: ci => ci.ProjectWorkingId, predicate: ci => ci.Id == constructionItemId);
        if (item == Guid.Empty)
            throw new KeyNotFoundException($"Không tìm thấy construction item với id {constructionItemId}.");
        return item;
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    private static ChecklistStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;

        if (!Enum.TryParse<ChecklistStatus>(status.Trim(), ignoreCase: true, out var parsed))
            throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, passed, failed.");

        return parsed;
    }

    private static Func<IQueryable<ChecklistItem>, IIncludableQueryable<ChecklistItem, object>> BuildInclude() =>
        q => q.Include(c => c.Design!).Include(c => c.ConstructionItem!);

    private async Task<ChecklistItem> LoadAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id, include: BuildInclude())
        ?? throw new KeyNotFoundException($"Không tìm thấy mục nghiệm thu với id {id}.");
}
