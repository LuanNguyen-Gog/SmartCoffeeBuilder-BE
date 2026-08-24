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
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? constructionItemId = null, string? status = null, Guid? projectWorkingId = null)
    {
        ItemStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ItemStatus>(status, ignoreCase: true, out var parsed))
                throw new ArgumentException($"Status '{status}' is not valid. Allowed: pending, in_progress, completed.");
            st = parsed;
        }

        // Lọc TRONG query (null = admin, xem tất cả) — lọc sau khi lấy về sẽ làm sai TotalItems.
        var visibleEngagementIds = await EngagementAuthorization
            .GetVisibleEngagementIdsAsync(_unitOfWork, accountId);

        var query = _repository
            .GetQueryable(e => (constructionItemId == null || e.ConstructionItemId == constructionItemId)
                               // Task không giữ engagement id — đi qua milestone cha (LEFT JOIN).
                               && (projectWorkingId == null
                                   || e.ConstructionItem.ProjectWorkingId == projectWorkingId)
                               && (st == null || e.Status == st)
                               && (visibleEngagementIds == null
                                   || visibleEngagementIds.Contains(e.ConstructionItem.ProjectWorkingId)))
            // Xuôi theo mốc thời gian, cùng lý do với ConstructionItemService: áp mẫu quy trình
            // ghi mọi việc con trong một transaction nên chúng dùng chung một CreatedAt, sắp theo
            // cột đó là thứ tự tuỳ ý — trong một hạng mục MEP, "thử áp lực nước" hiện trước "đi
            // ống điện âm tường". ThenBy Id để hai việc cùng hạn vẫn ổn định qua các trang.
            .OrderBy(e => e.EstimateAt)
            .ThenBy(e => e.CreatedAt)
            .ThenBy(e => e.Id);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ConstructionTaskResponse>(
            paged.Items.Select(ConstructionTaskResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ConstructionTaskResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var task = await LoadForActionAsync(accountId, id, "view construction tasks",
            EngagementActor.Owner, EngagementActor.Provider);

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> CreateAsync(
        Guid accountId, CreateConstructionTaskRequest request)
    {
        var item = await _unitOfWork.GetRepository<ConstructionItem>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ConstructionItemId)
            ?? throw new KeyNotFoundException($"No construction item found with id {request.ConstructionItemId}.");

        // Quyền trước guard nghiệp vụ: người ngoài không được dò trạng thái milestone qua câu lỗi.
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, item.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, "create a construction task", EngagementActor.Provider);

        if (item.Status == ItemStatus.completed)
            throw new InvalidOperationException("This milestone is already 'completed' — no more tasks can be added.");

        ConstructionSchedule.EnsureEstimateNotInPast(request.EstimateAt, "the task");
        ConstructionSchedule.EnsureRangeOrdered(request.StartAt, request.EstimateAt, "the task");
        EnsureCostNotNegative(request.EstimatedLaborCost, nameof(request.EstimatedLaborCost));

        var task = new ConstructionTask
        {
            ConstructionItemId = item.Id,
            Name = request.Name,
            Description = request.Description,
            // Ảnh phải upload qua api/files trước; giá trị gửi lên được rút về ObjectName.
            ImageUrl = await _fileStorage.NormalizeForStorageAsync(request.ImageUrl, "imageUrl"),
            StartAt = request.StartAt,
            EstimateAt = request.EstimateAt,
            EstimatedLaborCost = request.EstimatedLaborCost,
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
        Guid accountId, Guid id, UpdateConstructionTaskRequest request)
    {
        var task = await LoadForActionAsync(accountId, id, "edit a construction task", EngagementActor.Provider);

        if (task.Status == ItemStatus.completed)
            throw new InvalidOperationException("This task is already 'completed' — it can no longer be edited.");

        // Chỉ soi giá trị MỚI: hạn cũ đã lỡ trôi vào quá khứ vẫn sửa được các trường khác.
        if (request.EstimateAt.HasValue)
            ConstructionSchedule.EnsureEstimateNotInPast(request.EstimateAt, "the task");

        // So trên giá trị SAU KHI GHÉP với bản ghi hiện tại — xem ghi chú ở ConstructionItemService.
        ConstructionSchedule.EnsureRangeOrdered(
            request.StartAt ?? task.StartAt, request.EstimateAt ?? task.EstimateAt, "the task");
        ConstructionSchedule.EnsureRangeOrdered(
            request.ActualStartAt ?? task.ActualStartAt, request.ActualAt ?? task.ActualAt,
            "the task (actual)");

        EnsureCostNotNegative(request.EstimatedLaborCost, nameof(request.EstimatedLaborCost));
        EnsureCostNotNegative(request.ActualLaborCost, nameof(request.ActualLaborCost));

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

        if (request.StartAt.HasValue) task.StartAt = request.StartAt.Value;
        if (request.EstimateAt.HasValue) task.EstimateAt = request.EstimateAt.Value;
        if (request.ActualStartAt.HasValue) task.ActualStartAt = request.ActualStartAt.Value;
        if (request.ActualAt.HasValue) task.ActualAt = request.ActualAt.Value;
        if (request.EstimatedLaborCost.HasValue) task.EstimatedLaborCost = request.EstimatedLaborCost;
        if (request.ActualLaborCost.HasValue) task.ActualLaborCost = request.ActualLaborCost;
        if (request.Reason != null) task.Reason = request.Reason;
        task.UpdatedAt = DateTime.UtcNow;

        _repository.Update(task);
        await _unitOfWork.CommitAsync();

        await _fileStorage.TryDeleteAsync(replacedImage);

        return ConstructionTaskResponse.From(task);
    }

    public async Task<ConstructionTaskResponse> UpdateStatusAsync(
        Guid accountId, Guid id, UpdateConstructionTaskStatusRequest request)
    {
        if (!Enum.TryParse<ItemStatus>(request.Status, ignoreCase: true, out var target))
            throw new ArgumentException($"Status '{request.Status}' is not valid. Allowed: pending, in_progress, completed.");

        var task = await LoadForActionAsync(
            accountId, id, "update construction task progress", EngagementActor.Provider);

        // pending → in_progress → completed (chỉ tiến, không lùi).
        var allowed = task.Status switch
        {
            ItemStatus.pending => target == ItemStatus.in_progress,
            ItemStatus.in_progress => target == ItemStatus.completed,
            _ => false
        };
        if (!allowed)
            throw new InvalidOperationException($"A task cannot move from '{task.Status}' to '{target}'.");

        task.Status = target;

        // Mốc thực tế tự đóng theo trạng thái (xem ConstructionItemService.UpdateStatusAsync).
        if (target == ItemStatus.in_progress && task.ActualStartAt == null)
            task.ActualStartAt = VietnamTime.Today;
        if (target == ItemStatus.completed && task.ActualAt == null)
            task.ActualAt = VietnamTime.Today;
        if (target == ItemStatus.completed && task.ActualStartAt == null)
            task.ActualStartAt = task.ActualAt;
        task.UpdatedAt = DateTime.UtcNow;

        _repository.Update(task);
        await _unitOfWork.CommitAsync();

        return ConstructionTaskResponse.From(task);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var task = await LoadForActionAsync(accountId, id, "delete a construction task", EngagementActor.Provider);

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
        Guid accountId, Guid id, string action, params EngagementActor[] allowed)
    {
        var task = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q.Include(e => e.ConstructionItem))
            ?? throw new KeyNotFoundException($"No construction task found with id {id}.");

        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, task.ConstructionItem.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, action, allowed);

        return task;
    }

    /// <summary>Chi phí âm là dữ liệu sai — bỏ trống nếu chưa biết.</summary>
    /// <exception cref="ArgumentException">Giá trị âm (HTTP 400).</exception>
    private static void EnsureCostNotNegative(decimal? value, string fieldName)
    {
        if (value is decimal v && v < 0)
            throw new ArgumentException($"{fieldName} cannot be negative — leave it empty if there is no figure yet.");
    }
}
