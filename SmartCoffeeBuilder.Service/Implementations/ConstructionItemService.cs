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
    private readonly INotificationService _notificationService;

    public ConstructionItemService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ConstructionItem>();
        _notificationService = notificationService;
    }

    public async Task<PaginationResponse<ConstructionItemResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, Guid? parentId = null, string? status = null)
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
            // Theo MỐC THỜI GIAN, tăng dần — đây là tiến độ thi công, đọc xuôi theo lịch mới có
            // nghĩa. Trước đây sắp theo CreatedAt giảm dần, và điều đó hỏng hẳn khi áp mẫu quy
            // trình: ApplyAsync ghi cả bộ hạng mục trong một transaction nên chúng dùng CHUNG một
            // CreatedAt tới từng mili-giây, khoá sắp xếp không phân biệt được hàng nào với hàng
            // nào, Postgres trả về thứ tự tuỳ ý và màn kế hoạch hiện lộn xộn (Sơn nước trước Phần
            // thô). Cả app chủ quán lẫn web nhà cung cấp đều tin thứ tự của server nên đều sai.
            //
            // NULL (hạng mục chưa đặt hạn) xuống cuối theo mặc định NULLS LAST của Postgres cho
            // ASC — đúng ý: việc chưa có mốc thì chưa nằm trên lịch.
            //
            // ThenBy Id là chốt chặn cuối: hai hạng mục cùng ngày vẫn phải ra cùng một thứ tự ở
            // mọi lần gọi, nếu không phân trang sẽ lặp hoặc bỏ sót hàng giữa hai trang.
            .OrderBy(e => e.EstimateAt)
            .ThenBy(e => e.CreatedAt)
            .ThenBy(e => e.Id);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ConstructionItemResponse>(
            paged.Items.Select(ConstructionItemResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ConstructionItemResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var item = await LoadForActionAsync(accountId, id, "xem hạng mục thi công",
            EngagementActor.Owner, EngagementActor.Provider);

        return ConstructionItemResponse.From(item);
    }

    public async Task<ConstructionItemResponse> CreateAsync(Guid accountId, CreateConstructionItemRequest request)
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

        ConstructionSchedule.EnsureEstimateNotInPast(request.EstimateAt, "hạng mục");
        ConstructionSchedule.EnsureRangeOrdered(request.StartAt, request.EstimateAt, "hạng mục");
        EnsureCostNotNegative(request.EstimatedLaborCost, nameof(request.EstimatedLaborCost));

        if (request.ParentId != null)
        {
            var parent = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == request.ParentId)
                ?? throw new KeyNotFoundException($"Không tìm thấy milestone cha với id {request.ParentId}.");
            if (parent.ProjectWorkingId != engagement.Id)
                throw new InvalidOperationException("Milestone cha phải thuộc cùng engagement.");

            // Thi công đúng 2 CẤP (v5): milestone gốc → milestone con → task. Bản thân milestone con
            // KHÔNG được làm cha tiếp, nếu không cây đào sâu tuỳ ý trong khi FE chỉ render 2 tầng
            // và các guard bên dưới (đóng milestone / nghiệm thu) chỉ nhìn đúng một mức con.
            if (parent.ParentId != null)
                throw new InvalidOperationException(
                    "Thi công chỉ có 2 cấp milestone — milestone con không thể làm milestone cha. " +
                    "Gắn vào milestone gốc, hoặc tạo task bên trong milestone con này.");
        }

        var item = new ConstructionItem
        {
            ProjectWorkingId = engagement.Id,
            ParentId = request.ParentId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            StartAt = request.StartAt,
            EstimateAt = request.EstimateAt,
            EstimatedLaborCost = request.EstimatedLaborCost,
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
        Guid accountId, Guid id, UpdateConstructionItemRequest request)
    {
        var item = await LoadForActionAsync(accountId, id, "sửa hạng mục thi công", EngagementActor.Provider);

        if (item.Status == ItemStatus.completed)
            throw new InvalidOperationException("Hạng mục đã 'completed' — không chỉnh sửa được nữa.");

        // Chỉ soi giá trị MỚI: hạn cũ đã lỡ trôi vào quá khứ vẫn sửa được các trường khác.
        if (request.EstimateAt.HasValue)
            ConstructionSchedule.EnsureEstimateNotInPast(request.EstimateAt, "hạng mục");

        // Kiểm tra thứ tự trên giá trị SAU KHI GHÉP: gửi mỗi StartAt mà so với request.EstimateAt
        // (đang null) thì mọi ngày bắt đầu đều lọt, kể cả ngày nằm sau hạn đã lưu trong DB.
        ConstructionSchedule.EnsureRangeOrdered(
            request.StartAt ?? item.StartAt, request.EstimateAt ?? item.EstimateAt, "hạng mục");
        ConstructionSchedule.EnsureRangeOrdered(
            request.ActualStartAt ?? item.ActualStartAt, request.ActualAt ?? item.ActualAt,
            "hạng mục (thực tế)");

        EnsureCostNotNegative(request.EstimatedLaborCost, nameof(request.EstimatedLaborCost));
        EnsureCostNotNegative(request.ActualLaborCost, nameof(request.ActualLaborCost));

        if (request.Name != null) item.Name = request.Name;
        if (request.Description != null) item.Description = request.Description;
        if (request.Category != null) item.Category = request.Category;
        if (request.StartAt.HasValue) item.StartAt = request.StartAt.Value;
        if (request.EstimateAt.HasValue) item.EstimateAt = request.EstimateAt.Value;
        if (request.ActualStartAt.HasValue) item.ActualStartAt = request.ActualStartAt.Value;
        if (request.ActualAt.HasValue) item.ActualAt = request.ActualAt.Value;
        if (request.EstimatedLaborCost.HasValue) item.EstimatedLaborCost = request.EstimatedLaborCost;
        if (request.ActualLaborCost.HasValue) item.ActualLaborCost = request.ActualLaborCost;
        item.UpdatedAt = DateTime.UtcNow;

        _repository.Update(item);
        await _unitOfWork.CommitAsync();

        return ConstructionItemResponse.From(item);
    }

    public async Task<ConstructionItemResponse> UpdateStatusAsync(
        Guid accountId, Guid id, UpdateConstructionItemStatusRequest request)
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

        // Milestone chỉ đóng được khi MỌI thứ treo dưới nó đã xong — cả task lẫn milestone con.
        // Thiếu bước này thì cây thi công hiện tick xanh cho phần việc còn dở, và guard nghiệm thu
        // của ProjectWorkingService (mọi milestone 'completed') mất một lớp đối chứng.
        if (target == ItemStatus.completed)
        {
            var unfinishedTasks = await _unitOfWork.GetRepository<ConstructionTask>()
                .CountAsync(t => t.ConstructionItemId == item.Id && t.Status != ItemStatus.completed);
            if (unfinishedTasks > 0)
                throw new InvalidOperationException(
                    $"Còn {unfinishedTasks} task chưa 'completed' trong hạng mục này — " +
                    "hoàn thành hoặc xoá hết task trước khi đóng milestone.");

            // Chỉ cần soi ĐÚNG MỘT mức con: cây bị chặn ở 2 cấp nên milestone con không có con nữa.
            var unfinishedChildren = await _repository.CountAsync(
                c => c.ParentId == item.Id && c.Status != ItemStatus.completed);
            if (unfinishedChildren > 0)
                throw new InvalidOperationException(
                    $"Còn {unfinishedChildren} milestone con chưa 'completed' — " +
                    "đóng hết milestone con trước khi đóng milestone cha.");

            // Đóng milestone = tuyên bố phần việc này đã xong, nên checklist nghiệm thu của nó
            // phải được owner chấm đạt trước (review 3).
            await ChecklistGate.EnsureConstructionItemPassedAsync(
                _unitOfWork, item.Id, "chưa đóng được hạng mục này");
        }

        item.Status = target;

        // Mốc thực tế tự đóng theo trạng thái, cùng cách ActualAt vẫn làm: bắt provider nhớ điền
        // tay ngày bắt đầu thì cột đó rỗng ở phần lớn hạng mục và không tính được thời lượng THẬT.
        if (target == ItemStatus.in_progress && item.ActualStartAt == null)
            item.ActualStartAt = DateOnly.FromDateTime(DateTime.UtcNow);
        if (target == ItemStatus.completed && item.ActualAt == null)
            item.ActualAt = DateOnly.FromDateTime(DateTime.UtcNow);

        // Nhảy thẳng pending → completed (hạng mục làm gọn trong ngày) vẫn phải có mốc bắt đầu,
        // nếu không thời lượng thực tế thành null trong khi việc rõ ràng đã làm xong.
        if (target == ItemStatus.completed && item.ActualStartAt == null)
            item.ActualStartAt = item.ActualAt;

        item.UpdatedAt = DateTime.UtcNow;

        _repository.Update(item);
        await _unitOfWork.CommitAsync();

        return ConstructionItemResponse.From(item);
    }

    /// <summary>Chi phí âm là dữ liệu sai — bỏ trống nếu chưa biết.</summary>
    /// <exception cref="ArgumentException">Giá trị âm (HTTP 400).</exception>
    private static void EnsureCostNotNegative(decimal? value, string fieldName)
    {
        if (value is decimal v && v < 0)
            throw new ArgumentException($"{fieldName} không được âm — bỏ trống nếu chưa có số liệu.");
    }

    // ───────────────────────── Tổng hợp chi phí (review 1.1) ─────────────────────────

    public async Task<ConstructionCostSummaryResponse> GetCostSummaryAsync(Guid accountId, Guid id)
    {
        var item = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy hạng mục thi công với id {id}.");

        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, item.ProjectWorkingId);

        var ctx = await LoadCostContextAsync(item.ProjectWorkingId);
        return BuildSummary(item, ctx);
    }

    public async Task<EngagementCostSummaryResponse> GetEngagementCostSummaryAsync(
        Guid accountId, Guid projectWorkingId)
    {
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, projectWorkingId);

        var ctx = await LoadCostContextAsync(projectWorkingId);

        // Chỉ cộng milestone GỐC: milestone con đã nằm trong tổng của cha nó, cộng cả hai là
        // tính trùng toàn bộ phần con.
        var roots = ctx.Items.Where(i => i.ParentId == null).OrderBy(i => i.CreatedAt).ToList();
        var summaries = roots.Select(r => BuildSummary(r, ctx)).ToList();

        // Tách nhân công / vật tư ở mức engagement phải đi HẾT cây: EstimatedLaborCost trên mỗi
        // summary chỉ là phần của riêng milestone đó, phần của milestone con nằm trong
        // ChildrenEstimatedCost (đã trộn cả nhân công lẫn vật tư nên không tách ngược ra được).
        static decimal SumEstimatedLabor(ConstructionCostSummaryResponse s) =>
            s.EstimatedLaborCost + s.Children.Sum(SumEstimatedLabor);
        static decimal SumEstimatedMaterial(ConstructionCostSummaryResponse s) =>
            s.EstimatedMaterialCost + s.Children.Sum(SumEstimatedMaterial);

        var missingLabor = summaries.Sum(s => s.MissingActualLaborLines);
        var missingMaterial = summaries.Sum(s => s.MissingActualMaterialLines);

        static decimal SumActualLabor(ConstructionCostSummaryResponse s) =>
            (s.ActualLaborCost ?? 0m) + s.Children.Sum(SumActualLabor);
        static decimal SumActualMaterial(ConstructionCostSummaryResponse s) =>
            (s.ActualMaterialCost ?? 0m) + s.Children.Sum(SumActualMaterial);

        var estimated = summaries.Sum(s => s.TotalEstimatedCost);
        var actual = summaries.All(s => s.TotalActualCost.HasValue)
            ? summaries.Sum(s => s.TotalActualCost!.Value)
            : (decimal?)null;

        // Phát sinh đã duyệt là chi phí thật của hợp tác này, chỉ không nằm trong cây hạng mục.
        // Kéo vào đây để một màn hình trả lời trọn câu "dự án này tốn bao nhiêu".
        var changeOrders = await _unitOfWork.GetRepository<ChangeOrder>().GetListAsync(
            selector: c => new { c.Status, c.Amount },
            predicate: c => c.ProjectWorkingId == projectWorkingId);

        var acceptedChangeOrders = changeOrders
            .Where(c => c.Status == ChangeOrderStatus.accepted).Sum(c => c.Amount);

        return new EngagementCostSummaryResponse
        {
            ProjectWorkingId = projectWorkingId,
            EstimatedLaborCost = summaries.Sum(SumEstimatedLabor),
            ActualLaborCost = missingLabor > 0 ? null : summaries.Sum(SumActualLabor),
            EstimatedMaterialCost = summaries.Sum(SumEstimatedMaterial),
            ActualMaterialCost = missingMaterial > 0 ? null : summaries.Sum(SumActualMaterial),
            TotalEstimatedCost = estimated,
            TotalActualCost = actual,
            Variance = actual.HasValue ? actual.Value - estimated : null,
            MissingActualMaterialLines = missingMaterial,
            MissingActualLaborLines = missingLabor,
            RootItemCount = roots.Count,
            Items = summaries,
            AcceptedChangeOrderAmount = acceptedChangeOrders,
            PendingChangeOrderAmount = changeOrders
                .Where(c => c.Status == ChangeOrderStatus.pending).Sum(c => c.Amount),
            TotalEstimatedCostWithChangeOrders = estimated + acceptedChangeOrders
        };
    }

    /// <summary>
    /// Toàn bộ hạng mục, task và dòng vật tư của một engagement, nạp MỘT lần.
    /// Cây thi công nhỏ (vài chục dòng) nên nạp cả rồi gộp trong bộ nhớ rẻ hơn nhiều so với
    /// đệ quy xuống DB cho từng milestone.
    /// </summary>
    private async Task<CostContext> LoadCostContextAsync(Guid projectWorkingId)
    {
        var items = await _repository.GetListAsync(predicate: i => i.ProjectWorkingId == projectWorkingId);
        var itemIds = items.Select(i => i.Id).ToHashSet();

        var tasks = await _unitOfWork.GetRepository<ConstructionTask>()
            .GetListAsync(predicate: t => itemIds.Contains(t.ConstructionItemId));
        var taskIds = tasks.Select(t => t.Id).ToHashSet();

        var materials = await _unitOfWork.GetRepository<ConstructionMaterial>()
            .GetListAsync(predicate: m =>
                (m.ConstructionItemId != null && itemIds.Contains(m.ConstructionItemId.Value))
                || (m.ConstructionTaskId != null && taskIds.Contains(m.ConstructionTaskId.Value)));

        return new CostContext([.. items], [.. tasks], [.. materials]);
    }

    private static ConstructionCostSummaryResponse BuildSummary(ConstructionItem item, CostContext ctx)
    {
        var tasks = ctx.Tasks.Where(t => t.ConstructionItemId == item.Id).ToList();
        var taskIds = tasks.Select(t => t.Id).ToHashSet();

        var materialLines = ctx.Materials
            .Where(m => m.ConstructionItemId == item.Id
                        || (m.ConstructionTaskId != null && taskIds.Contains(m.ConstructionTaskId.Value)))
            .ToList();

        // Nhân công: cột trên chính hạng mục + cột trên từng task con.
        var laborSources = new List<(decimal? Estimated, decimal? Actual)>
        {
            (item.EstimatedLaborCost, item.ActualLaborCost)
        };
        laborSources.AddRange(tasks.Select(t => (t.EstimatedLaborCost, t.ActualLaborCost)));

        // Dòng nào ĐÃ khai dự toán mà chưa có thực chi thì tổng thực tế không đọc được — đếm lại
        // để FE giải thích được vì sao Actual* null. Dòng chưa khai gì cả không tính là thiếu.
        var missingLabor = laborSources.Count(s => s.Estimated.HasValue && !s.Actual.HasValue);
        var estimatedLabor = laborSources.Sum(s => s.Estimated ?? 0m);
        var actualLabor = missingLabor > 0 ? (decimal?)null : laborSources.Sum(s => s.Actual ?? 0m);

        var missingMaterial = materialLines.Count(m => m.ActualQuantity == null);
        var estimatedMaterial = materialLines.Sum(m => m.EstimatedQuantity * m.UnitPrice);
        var actualMaterial = missingMaterial > 0
            ? (decimal?)null
            : materialLines.Sum(m => (m.ActualQuantity ?? 0m) * m.UnitPrice);

        var ownEstimated = estimatedLabor + estimatedMaterial;
        var ownActual = actualLabor.HasValue && actualMaterial.HasValue
            ? actualLabor.Value + actualMaterial.Value
            : (decimal?)null;

        // Cây thi công bị chặn ở 2 cấp, nên đệ quy này sâu tối đa một tầng — không có vòng lặp vô tận.
        var children = ctx.Items
            .Where(c => c.ParentId == item.Id)
            .OrderBy(c => c.CreatedAt)
            .Select(c => BuildSummary(c, ctx))
            .ToList();

        var childrenEstimated = children.Sum(c => c.TotalEstimatedCost);
        var childrenActual = children.Count > 0 && children.All(c => c.TotalActualCost.HasValue)
            ? children.Sum(c => c.TotalActualCost!.Value)
            : children.Count == 0 ? 0m : (decimal?)null;

        var totalEstimated = ownEstimated + childrenEstimated;
        var totalActual = ownActual.HasValue && childrenActual.HasValue
            ? ownActual.Value + childrenActual.Value
            : (decimal?)null;

        return new ConstructionCostSummaryResponse
        {
            ConstructionItemId = item.Id,
            Name = item.Name,
            Category = item.Category,
            Status = item.Status.ToString(),
            EstimatedLaborCost = estimatedLabor,
            ActualLaborCost = actualLabor,
            EstimatedMaterialCost = estimatedMaterial,
            ActualMaterialCost = actualMaterial,
            EstimatedCost = ownEstimated,
            ActualCost = ownActual,
            ChildrenEstimatedCost = childrenEstimated,
            ChildrenActualCost = childrenActual,
            TotalEstimatedCost = totalEstimated,
            TotalActualCost = totalActual,
            Variance = totalActual.HasValue ? totalActual.Value - totalEstimated : null,
            MissingActualMaterialLines = missingMaterial + children.Sum(c => c.MissingActualMaterialLines),
            MissingActualLaborLines = missingLabor + children.Sum(c => c.MissingActualLaborLines),
            StartAt = item.StartAt,
            EstimateAt = item.EstimateAt,
            PlannedDurationDays = ConstructionSchedule.DurationDays(item.StartAt, item.EstimateAt),
            ActualDurationDays = ConstructionSchedule.DurationDays(item.ActualStartAt, item.ActualAt),
            Children = children
        };
    }

    /// <summary>Ảnh chụp toàn bộ dữ liệu chi phí của một engagement, dùng chung cho mọi mức gộp.</summary>
    private sealed record CostContext(
        List<ConstructionItem> Items,
        List<ConstructionTask> Tasks,
        List<ConstructionMaterial> Materials);

    public async Task DeleteAsync(Guid accountId, Guid id)
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

    public async Task<int> NotifyOverdueProgressAsync(int renotifyAfterDays = 7)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Chỉ quét cấp MILESTONE, không quét task: task nằm trong milestone nên một hạng mục trễ
        // sẽ kéo theo cả loạt task trễ, báo cả hai cấp là dội cùng một tin nhiều lần.
        // Chỉ engagement còn sống mới có nghĩa — dự án đã nghiệm thu/huỷ ngang thì cảnh báo trễ
        // chỉ là rác trong hộp thư.
        var overdueIds = await _repository.GetListAsync(
            selector: ci => ci.Id,
            predicate: ci => ci.EstimateAt != null
                             && ci.EstimateAt < today
                             && ci.Status != ItemStatus.completed
                             && ci.ProjectWorking.Status == ProviderStatus.accepted);

        if (overdueIds.Count == 0) return 0;

        var sent = 0;
        foreach (var id in overdueIds)
        {
            // Best-effort từng hạng mục: một owner không có email / noti lỗi không được làm hỏng
            // cả lượt quét của những hạng mục còn lại.
            if (await _notificationService.NotifyConstructionOverdueAsync(id, renotifyAfterDays))
                sent++;
        }

        return sent;
    }

    /// <summary>
    /// Nạp milestone và chốt quyền trong một bước — mọi endpoint theo id đều phải đi qua đây.
    /// </summary>
    private async Task<ConstructionItem> LoadForActionAsync(
        Guid accountId, Guid id, string action, params EngagementActor[] allowed)
    {
        var item = await _repository.SingleOrDefaultAsync(predicate: e => e.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy construction item với id {id}.");

        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, item.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, action, allowed);

        return item;
    }
}
