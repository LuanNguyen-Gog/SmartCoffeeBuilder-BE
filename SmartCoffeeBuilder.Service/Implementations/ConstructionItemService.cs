using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Milestone thi công. Mọi endpoint đều đi qua ownership check theo ENGAGEMENT
/// (<see cref="EngagementAuthorization"/>): trạng thái các milestone chính là dữ liệu mà
/// <c>ProjectWorkingService</c> dùng để phán engagement đã xong việc chưa, nên ai cũng ghi được
/// vào đây thì guard nghiệm thu mất giá trị.
/// </summary>
public class ConstructionItemService : IConstructionItemService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ConstructionItem> _repository;

    public ConstructionItemService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ConstructionItem>();
    }

    public async Task<PaginationResponse<ConstructionItemResponse>> GetAllAsync(
        long accountId, int pageNumber = 1, int pageSize = 10,
        long? projectWorkingId = null, long? parentId = null, string? status = null)
    {
        ItemStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ItemStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, in_progress, completed.");
            st = parsed;
        }

        // Lọc TRONG query, không lọc sau khi lấy về — lọc sau làm sai TotalItems của phân trang.
        // null = admin, xem tất cả.
        var visibleEngagementIds = await EngagementAuthorization
            .GetVisibleEngagementIdsAsync(_unitOfWork, accountId);

        var query = _repository
            .GetQueryable(e => (projectWorkingId == null || e.ProjectWorkingId == projectWorkingId)
                               && (parentId == null || e.ParentId == parentId)
                               && (st == null || e.Status == st)
                               && (visibleEngagementIds == null
                                   || visibleEngagementIds.Contains(e.ProjectWorkingId)))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ConstructionItemResponse>(
            paged.Items.Select(ConstructionItemResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ConstructionItemResponse> GetByIdAsync(long accountId, long id)
    {
        var item = await LoadForActionAsync(accountId, id, "xem hạng mục thi công",
            EngagementActor.Owner, EngagementActor.Provider);

        return ConstructionItemResponse.From(item);
    }

    public async Task<ConstructionItemResponse> CreateAsync(long accountId, CreateConstructionItemRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectWorkingId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {request.ProjectWorkingId}.");

        // Nhà thầu là người lập milestone của chính mình — owner không tự thêm việc vào phần
        // của provider. Quyền check TRƯỚC mọi guard nghiệp vụ để endpoint không rò rỉ trạng thái
        // engagement của người khác qua thông báo lỗi.
        var actor = await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagement.Id);
        EngagementAuthorization.EnsureActor(actor, "tạo hạng mục thi công", EngagementActor.Provider);

        if (engagement.ContractType == ServiceKind.design)
            throw new InvalidOperationException(
                "Engagement có contract type 'design' — không có giai đoạn thi công.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ tạo hạng mục thi công khi engagement 'accepted'.");

        // v5: "đã ký mới được làm" — guard qua contract confirmed, không check provider_status.
        var hasConfirmedContract = await _unitOfWork.GetRepository<Contract>()
            .CountAsync(c => c.ProjectWorkingId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
        if (!hasConfirmedContract)
            throw new InvalidOperationException(
                "Engagement chưa có contract 'confirmed' — ký hợp đồng trước khi tạo hạng mục thi công.");

        if (request.ParentId != null)
        {
            var parent = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == request.ParentId)
                ?? throw new KeyNotFoundException($"Không tìm thấy milestone cha với id {request.ParentId}.");
            if (parent.ProjectWorkingId != engagement.Id)
                throw new InvalidOperationException("Milestone cha phải thuộc cùng engagement.");
        }

        var item = new ConstructionItem
        {
            ProjectWorkingId = engagement.Id,
            ParentId = request.ParentId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            EstimateAt = request.EstimateAt,
            Status = ItemStatus.pending,
            // Người tạo lấy từ JWT, KHÔNG nhận từ body: client tự khai thì cột này mất giá trị đối chứng.
            CreatedBy = accountId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(item);
        await _unitOfWork.CommitAsync();

        return ConstructionItemResponse.From(item);
    }

    public async Task<ConstructionItemResponse> UpdateAsync(
        long accountId, long id, UpdateConstructionItemRequest request)
    {
        var item = await LoadForActionAsync(accountId, id, "sửa hạng mục thi công", EngagementActor.Provider);

        if (item.Status == ItemStatus.completed)
            throw new InvalidOperationException("Hạng mục đã 'completed' — không chỉnh sửa được nữa.");

        if (request.Name != null) item.Name = request.Name;
        if (request.Description != null) item.Description = request.Description;
        if (request.Category != null) item.Category = request.Category;
        if (request.EstimateAt.HasValue) item.EstimateAt = request.EstimateAt.Value;
        item.UpdatedAt = DateTime.UtcNow;

        _repository.Update(item);
        await _unitOfWork.CommitAsync();

        return ConstructionItemResponse.From(item);
    }

    public async Task<ConstructionItemResponse> UpdateStatusAsync(
        long accountId, long id, UpdateConstructionItemStatusRequest request)
    {
        if (!Enum.TryParse<ItemStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: pending, in_progress, completed.");

        // Chỉ nhà thầu của chính engagement báo tiến độ — đây là dữ liệu guard nghiệm thu tin vào.
        var item = await LoadForActionAsync(
            accountId, id, "cập nhật tiến độ hạng mục thi công", EngagementActor.Provider);

        // pending → in_progress → completed (chỉ tiến, không lùi, không cancel).
        // TODO (Mục 10 — construction_dependency, đang DRAFT): khi chốt bảng phụ thuộc,
        // chặn pending→in_progress nếu còn milestone tiền đề chưa 'completed'.
        var allowed = item.Status switch
        {
            ItemStatus.pending => target == ItemStatus.in_progress,
            ItemStatus.in_progress => target == ItemStatus.completed,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException($"Không thể chuyển hạng mục từ '{item.Status}' sang '{target}'.");

        item.Status = target;
        if (target == ItemStatus.completed && item.ActualAt == null)
            item.ActualAt = DateOnly.FromDateTime(DateTime.UtcNow);
        item.UpdatedAt = DateTime.UtcNow;

        _repository.Update(item);
        await _unitOfWork.CommitAsync();

        return ConstructionItemResponse.From(item);
    }

    public async Task DeleteAsync(long accountId, long id)
    {
        var item = await LoadForActionAsync(accountId, id, "xoá hạng mục thi công", EngagementActor.Provider);

        var hasChildren = await _repository.CountAsync(e => e.ParentId == id) > 0;
        if (hasChildren)
            throw new InvalidOperationException("Hạng mục còn milestone con — xoá/di chuyển con trước khi xoá.");

        // Cascade xoá comment gắn vào milestone này (FK mềm — không tự cascade theo DB).
        var commentRepo = _unitOfWork.GetRepository<Comment>();
        var comments = await commentRepo.GetListAsync(
            predicate: c => c.TargetType == CommentTargetType.construction_item && c.TargetId == id);
        commentRepo.DeleteRange(comments);

        _repository.Delete(item); // task con cascade; issue liên quan set null construction_item_id.
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Nạp milestone và chốt quyền trong một bước — mọi endpoint theo id đều phải đi qua đây.
    /// </summary>
    private async Task<ConstructionItem> LoadForActionAsync(
        long accountId, long id, string action, params EngagementActor[] allowed)
    {
        var item = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {id}.");

        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, item.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, action, allowed);

        return item;
    }
}
