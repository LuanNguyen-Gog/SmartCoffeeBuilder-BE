using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTask;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Task trong milestone thi công. Quyền xét theo ENGAGEMENT của milestone cha
/// (<see cref="EngagementAuthorization"/>) — giống <see cref="ConstructionItemService"/>.
/// </summary>
public class ConstructionTaskService : IConstructionTaskService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ConstructionTask> _repository;
    private readonly IFileStorageService _fileStorage;

    public ConstructionTaskService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ConstructionTask>();
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<ConstructionTaskResponse>> GetAllAsync(
        long accountId, int pageNumber = 1, int pageSize = 10,
        long? constructionItemId = null, string? status = null)
    {
        ItemStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ItemStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' không hợp lệ. Cho phép: pending, in_progress, completed.");
            st = parsed;
        }

        // Lọc TRONG query (null = admin, xem tất cả) — lọc sau khi lấy về sẽ làm sai TotalItems.
        var visibleEngagementIds = await EngagementAuthorization
            .GetVisibleEngagementIdsAsync(_unitOfWork, accountId);

        var query = _repository
            .GetQueryable(e => (constructionItemId == null || e.ConstructionItemId == constructionItemId)
                               && (st == null || e.Status == st)
                               && (visibleEngagementIds == null
                                   || visibleEngagementIds.Contains(e.ConstructionItem.ProjectWorkingId)))
            .OrderByDescending(e => e.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ConstructionTaskResponse>(
            paged.Items.Select(ConstructionTaskResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ConstructionTaskResponse> GetByIdAsync(long accountId, long id)
    {
        var task = await LoadForActionAsync(accountId, id, "xem task thi công",
            EngagementActor.Owner, EngagementActor.Provider);

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> CreateAsync(
        long accountId, CreateConstructionTaskRequest request)
    {
        var item = await _unitOfWork.GetRepository<ConstructionItem>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ConstructionItemId)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {request.ConstructionItemId}.");

        // Quyền trước guard nghiệp vụ: người ngoài không được dò trạng thái milestone qua câu lỗi.
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, item.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, "tạo task thi công", EngagementActor.Provider);

        if (item.Status == ItemStatus.completed)
            throw new InvalidOperationException("Milestone đã 'completed' — không thêm task được nữa.");

        ConstructionSchedule.EnsureEstimateNotInPast(request.EstimateAt, "task");

        var task = new ConstructionTask
        {
            ConstructionItemId = item.Id,
            Name = request.Name,
            Description = request.Description,
            // Ảnh phải upload qua api/files trước; giá trị gửi lên được rút về ObjectName.
            ImageUrl = await _fileStorage.NormalizeForStorageAsync(request.ImageUrl, "imageUrl"),
            EstimateAt = request.EstimateAt,
            Status = ItemStatus.pending,
            // Người tạo lấy từ JWT, KHÔNG nhận từ body (xem CreateConstructionTaskRequest).
            CreatedBy = accountId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(task);
        await _unitOfWork.CommitAsync();

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> UpdateAsync(
        long accountId, long id, UpdateConstructionTaskRequest request)
    {
        var task = await LoadForActionAsync(accountId, id, "sửa task thi công", EngagementActor.Provider);

        if (task.Status == ItemStatus.completed)
            throw new InvalidOperationException("Task đã 'completed' — không chỉnh sửa được nữa.");

        // Chỉ soi giá trị MỚI: hạn cũ đã lỡ trôi vào quá khứ vẫn sửa được các trường khác.
        if (request.EstimateAt.HasValue)
            ConstructionSchedule.EnsureEstimateNotInPast(request.EstimateAt, "task");

        if (request.Name != null) task.Name = request.Name;
        if (request.Description != null) task.Description = request.Description;

        // Ảnh cũ bị thay thì dọn luôn object trên bucket (sau khi DB commit) để khỏi rác.
        string? replacedImage = null;
        if (request.ImageUrl != null)
        {
            var newImage = await _fileStorage.NormalizeForStorageAsync(request.ImageUrl, "imageUrl");
            if (newImage != task.ImageUrl) replacedImage = task.ImageUrl;
            task.ImageUrl = newImage;
        }

        if (request.EstimateAt.HasValue) task.EstimateAt = request.EstimateAt.Value;
        if (request.Reason != null) task.Reason = request.Reason;
        task.UpdatedAt = DateTime.UtcNow;

        _repository.Update(task);
        await _unitOfWork.CommitAsync();

        await _fileStorage.TryDeleteAsync(replacedImage);

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> UpdateStatusAsync(
        long accountId, long id, UpdateConstructionTaskStatusRequest request)
    {
        if (!Enum.TryParse<ItemStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' không hợp lệ. Cho phép: pending, in_progress, completed.");

        var task = await LoadForActionAsync(
            accountId, id, "cập nhật tiến độ task thi công", EngagementActor.Provider);

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

    public async Task DeleteAsync(long accountId, long id)
    {
        var task = await LoadForActionAsync(accountId, id, "xoá task thi công", EngagementActor.Provider);

        _repository.Delete(task);
        await _unitOfWork.CommitAsync();

        // Dọn ảnh hiện trường trên bucket sau khi DB đã commit.
        await _fileStorage.TryDeleteAsync(task.ImageUrl);
    }

    /// <summary>
    /// Nạp task và chốt quyền trong một bước. Task không giữ engagement id — phải đi qua milestone
    /// cha, nên include ConstructionItem thay vì query rời.
    /// </summary>
    private async Task<ConstructionTask> LoadForActionAsync(
        long accountId, long id, string action, params EngagementActor[] allowed)
    {
        var task = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.ConstructionItem))
            ?? throw new KeyNotFoundException($"Không tìm thấy construction task với id {id}.");

        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, task.ConstructionItem.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, action, allowed);

        return task;
    }
}
