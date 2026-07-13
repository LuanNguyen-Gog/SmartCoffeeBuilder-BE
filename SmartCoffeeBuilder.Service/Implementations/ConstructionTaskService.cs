using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTask;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

public class ConstructionTaskService : IConstructionTaskService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ConstructionTask> _repository;

    public ConstructionTaskService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ConstructionTask>();
    }

    public async Task<PaginationResponse<ConstructionTaskResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10,
        long? constructionItemId = null, string? status = null)
    {
        ItemStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ItemStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, in_progress, completed.");
            st = parsed;
        }

        var query = _repository
            .GetQueryable(e => (constructionItemId == null || e.ConstructionItemId == constructionItemId)
                               && (st == null || e.Status == st))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ConstructionTaskResponse>(
            paged.Items.Select(ConstructionTaskResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ConstructionTaskResponse> GetByIdAsync(long id)
    {
        var task = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction task với id {id}.");

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> CreateAsync(CreateConstructionTaskRequest request)
    {
        var item = await _unitOfWork.GetRepository<ConstructionItem>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ConstructionItemId)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {request.ConstructionItemId}.");

        if (item.Status == ItemStatus.completed)
            throw new InvalidOperationException("Milestone đã 'completed' — không thêm task được nữa.");

        if (request.CreatedBy != null)
        {
            _ = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == request.CreatedBy)
                ?? throw new KeyNotFoundException($"Không tìm thấy account với id {request.CreatedBy}.");
        }

        var task = new ConstructionTask
        {
            ConstructionItemId = item.Id,
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            EstimateAt = request.EstimateAt,
            Status = ItemStatus.pending,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(task);
        await _unitOfWork.CommitAsync();

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> UpdateAsync(long id, UpdateConstructionTaskRequest request)
    {
        var task = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction task với id {id}.");

        if (task.Status == ItemStatus.completed)
            throw new InvalidOperationException("Task đã 'completed' — không chỉnh sửa được nữa.");

        if (request.Name != null) task.Name = request.Name;
        if (request.Description != null) task.Description = request.Description;
        if (request.ImageUrl != null) task.ImageUrl = request.ImageUrl;
        if (request.EstimateAt.HasValue) task.EstimateAt = request.EstimateAt.Value;
        if (request.Reason != null) task.Reason = request.Reason;
        task.UpdatedAt = DateTime.UtcNow;

        _repository.Update(task);
        await _unitOfWork.CommitAsync();

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> UpdateStatusAsync(long id, UpdateConstructionTaskStatusRequest request)
    {
        if (!Enum.TryParse<ItemStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: pending, in_progress, completed.");

        var task = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction task với id {id}.");

        // pending → in_progress → completed (chỉ tiến, không lùi).
        var allowed = task.Status switch
        {
            ItemStatus.pending => target == ItemStatus.in_progress,
            ItemStatus.in_progress => target == ItemStatus.completed,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException($"Không thể chuyển task từ '{task.Status}' sang '{target}'.");

        task.Status = target;
        if (target == ItemStatus.completed && task.ActualAt == null)
            task.ActualAt = DateOnly.FromDateTime(DateTime.UtcNow);
        task.UpdatedAt = DateTime.UtcNow;

        _repository.Update(task);
        await _unitOfWork.CommitAsync();

        return ConstructionTaskResponse.From(task);
    }

    public async Task DeleteAsync(long id)
    {
        var task = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction task với id {id}.");

        _repository.Delete(task);
        await _unitOfWork.CommitAsync();
    }
}
