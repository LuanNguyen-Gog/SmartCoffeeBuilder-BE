using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

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
        int pageNumber = 1, int pageSize = 10,
        long? projectProviderId = null, long? parentId = null, string? status = null)
    {
        ItemStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ItemStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, in_progress, completed.");
            st = parsed;
        }

        var query = _repository
            .GetQueryable(e => (projectProviderId == null || e.ProjectProviderId == projectProviderId)
                               && (parentId == null || e.ParentId == parentId)
                               && (st == null || e.Status == st))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ConstructionItemResponse>(
            paged.Items.Select(ConstructionItemResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ConstructionItemResponse> GetByIdAsync(long id)
    {
        var item = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {id}.");

        return ConstructionItemResponse.From(item);
    }

    public async Task<ConstructionItemResponse> CreateAsync(CreateConstructionItemRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectProvider>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectProviderId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {request.ProjectProviderId}.");

        if (engagement.ContractType == ServiceKind.design)
            throw new InvalidOperationException(
                "Engagement có contract type 'design' — không có giai đoạn thi công.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ tạo hạng mục thi công khi engagement 'accepted'.");

        // v5: "đã ký mới được làm" — guard qua contract confirmed, không check provider_status.
        var hasConfirmedContract = await _unitOfWork.GetRepository<Contract>()
            .CountAsync(c => c.ProjectProviderId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
        if (!hasConfirmedContract)
            throw new InvalidOperationException(
                "Engagement chưa có contract 'confirmed' — ký hợp đồng trước khi tạo hạng mục thi công.");

        if (request.ParentId != null)
        {
            var parent = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == request.ParentId)
                ?? throw new KeyNotFoundException($"Không tìm thấy milestone cha với id {request.ParentId}.");
            if (parent.ProjectProviderId != engagement.Id)
                throw new InvalidOperationException("Milestone cha phải thuộc cùng engagement.");
        }

        if (request.CreatedBy != null)
        {
            _ = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == request.CreatedBy)
                ?? throw new KeyNotFoundException($"Không tìm thấy account với id {request.CreatedBy}.");
        }

        var item = new ConstructionItem
        {
            ProjectProviderId = engagement.Id,
            ParentId = request.ParentId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            EstimateAt = request.EstimateAt,
            Status = ItemStatus.pending,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(item);
        await _unitOfWork.CommitAsync();

        return ConstructionItemResponse.From(item);
    }

    public async Task<ConstructionItemResponse> UpdateAsync(long id, UpdateConstructionItemRequest request)
    {
        var item = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {id}.");

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

    public async Task<ConstructionItemResponse> UpdateStatusAsync(long id, UpdateConstructionItemStatusRequest request)
    {
        if (!Enum.TryParse<ItemStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: pending, in_progress, completed.");

        var item = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {id}.");

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

    public async Task DeleteAsync(long id)
    {
        var item = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {id}.");

        var hasChildren = await _repository.CountAsync(e => e.ParentId == id) > 0;
        if (hasChildren)
            throw new InvalidOperationException("Hạng mục còn milestone con — xoá/di chuyển con trước khi xoá.");

        _repository.Delete(item); // task con cascade; issue liên quan set null construction_item_id.
        await _unitOfWork.CommitAsync();
    }
}
