using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Material;
using SmartCoffeeBuilder.Service.DTOs.Responses.Material;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Vật tư thi công (review 3): "giá vật tư phải công bố trước khi làm — một danh sách vật tư kèm
/// tiền trên mỗi đơn vị; mỗi task và giai đoạn chọn ra từ danh sách đó rồi khai khối lượng; khối
/// lượng thực tế điền sau khi hoàn thành; milestone gộp khối lượng và giá của các task con".
///
/// Hai tầng tách bạch:
/// <list type="bullet">
/// <item><see cref="Material"/> — BẢNG GIÁ của engagement: có vật tư gì, bao nhiêu tiền/đơn vị.</item>
/// <item><see cref="ConstructionMaterial"/> — LƯỢNG DÙNG: hạng mục/task nào lấy vật tư nào, dự
/// tính bao nhiêu, thực tế bao nhiêu.</item>
/// </list>
/// Provider khai cả hai; owner chỉ đọc (tiền là thứ hai bên đã chốt trong báo giá/hợp đồng, không
/// để một bên sửa đơn phương).
/// </summary>
public class MaterialService : IMaterialService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Material> _repository;

    public MaterialService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Material>();
    }

    // ───────────────────────── Bảng giá ─────────────────────────

    public async Task<PaginationResponse<MaterialResponse>> GetAllAsync(
        Guid accountId, Guid projectWorkingId, int pageNumber = 1, int pageSize = 50)
    {
        // Đọc: cả hai bên của engagement đều xem được bảng giá.
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, projectWorkingId);

        var query = _repository
            .GetQueryable(m => m.ProjectWorkingId == projectWorkingId)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Name);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<MaterialResponse>(
            paged.Items.Select(MaterialResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<MaterialResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var material = await LoadAsync(id);
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, material.ProjectWorkingId);
        return MaterialResponse.From(material);
    }

    public async Task<MaterialResponse> CreateAsync(Guid accountId, CreateMaterialRequest request)
    {
        var engagement = await LoadEngagementAsync(request.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagement.Id),
            "declare the material price list", EngagementActor.Provider);

        // Cùng guard với tạo hạng mục thi công: bảng giá là một phần của kế hoạch thi công, mà kế
        // hoạch chỉ lập sau khi hợp đồng đã ký.
        await EnsureSignedContractAsync(engagement.Id);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("A material must have a name.");
        if (request.UnitPrice < 0)
            throw new ArgumentException($"The unit price of material '{request.Name}' cannot be negative.");

        var unit = ParseUnit(request.Unit);

        var duplicated = await _repository.CountAsync(
            m => m.ProjectWorkingId == engagement.Id && m.Name == request.Name.Trim()) > 0;
        if (duplicated)
            throw new InvalidOperationException(
                $"This engagement's price list already has a material named '{request.Name.Trim()}' — edit the existing row instead of adding a duplicate.");

        var now = DateTime.UtcNow;
        var existing = await _repository.GetListAsync(
            selector: m => m.SortOrder, predicate: m => m.ProjectWorkingId == engagement.Id);

        var material = new Material
        {
            ProjectWorkingId = engagement.Id,
            Name = request.Name.Trim(),
            Description = request.Description,
            Unit = unit,
            UnitPrice = request.UnitPrice,
            SortOrder = request.SortOrder ?? (existing.Count == 0 ? 0 : existing.Max() + 1),
            CreatedBy = accountId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.InsertAsync(material);
        await _unitOfWork.CommitAsync();

        return MaterialResponse.From(material);
    }

    public async Task<MaterialResponse> UpdateAsync(Guid accountId, Guid id, UpdateMaterialRequest request)
    {
        var material = await LoadAsync(id);
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, material.ProjectWorkingId),
            "edit the material price list", EngagementActor.Provider);

        if (request.Name != null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException("A material must have a name.");
            material.Name = request.Name.Trim();
        }
        if (request.Description != null) material.Description = request.Description;
        if (request.Unit != null) material.Unit = ParseUnit(request.Unit);
        if (request.SortOrder.HasValue) material.SortOrder = request.SortOrder.Value;

        if (request.UnitPrice.HasValue)
        {
            if (request.UnitPrice.Value < 0)
                throw new ArgumentException("The unit price cannot be negative.");

            // Đổi giá KHÔNG hồi tố: các dòng đã chốt giữ nguyên đơn giá đã sao chép lúc chọn vật
            // tư, nếu không thì mọi báo cáo chi phí cũ tự đổi số sau lưng người dùng.
            material.UnitPrice = request.UnitPrice.Value;
        }

        material.UpdatedAt = DateTime.UtcNow;
        _repository.Update(material);
        await _unitOfWork.CommitAsync();

        return MaterialResponse.From(material);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var material = await LoadAsync(id);
        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, material.ProjectWorkingId),
            "remove a material from the price list", EngagementActor.Provider);

        var used = await _unitOfWork.GetRepository<ConstructionMaterial>()
            .CountAsync(u => u.MaterialId == material.Id);
        if (used > 0)
            throw new InvalidOperationException(
                $"This material is used by {used} item(s)/task(s) — remove it from them before deleting it.");

        _repository.Delete(material);
        await _unitOfWork.CommitAsync();
    }

    // ───────────────────────── Lượng dùng ─────────────────────────

    public async Task<List<ConstructionMaterialResponse>> GetUsagesAsync(
        Guid accountId, Guid? constructionItemId, Guid? constructionTaskId)
    {
        if ((constructionItemId == null) == (constructionTaskId == null))
            throw new ArgumentException(
                "Send EXACTLY ONE of: constructionItemId or constructionTaskId.");

        var engagementId = constructionItemId != null
            ? (await LoadItemAsync(constructionItemId.Value)).ProjectWorkingId
            : (await LoadTaskAsync(constructionTaskId!.Value)).ConstructionItem.ProjectWorkingId;

        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagementId);

        var usages = await _unitOfWork.GetRepository<ConstructionMaterial>().GetListAsync(
            predicate: u => (constructionItemId != null && u.ConstructionItemId == constructionItemId)
                            || (constructionTaskId != null && u.ConstructionTaskId == constructionTaskId),
            include: q => q.Include(u => u.Material),
            orderBy: q => q.OrderBy(u => u.CreatedAt));

        return usages.Select(ConstructionMaterialResponse.From).ToList();
    }

    public async Task<ConstructionMaterialResponse> AddUsageAsync(
        Guid accountId, CreateConstructionMaterialRequest request)
    {
        if ((request.ConstructionItemId == null) == (request.ConstructionTaskId == null))
            throw new ArgumentException(
                "Send EXACTLY ONE of: constructionItemId (material counted at milestone level) " +
                "or constructionTaskId (material for a single task).");

        if (request.EstimatedQuantity <= 0)
            throw new ArgumentException("The estimated quantity must be greater than 0.");

        var engagementId = request.ConstructionItemId != null
            ? (await LoadItemAsync(request.ConstructionItemId.Value)).ProjectWorkingId
            : (await LoadTaskAsync(request.ConstructionTaskId!.Value)).ConstructionItem.ProjectWorkingId;

        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagementId),
            "declare materials for a construction item", EngagementActor.Provider);

        var material = await LoadAsync(request.MaterialId);

        // Vật tư phải thuộc ĐÚNG bảng giá của engagement đang thi công — nếu không thì đơn giá lấy
        // từ thoả thuận của một dự án khác.
        if (material.ProjectWorkingId != engagementId)
            throw new InvalidOperationException(
                "This material belongs to another engagement's price list — only use materials published for the current engagement.");

        var now = DateTime.UtcNow;
        var usage = new ConstructionMaterial
        {
            ConstructionItemId = request.ConstructionItemId,
            ConstructionTaskId = request.ConstructionTaskId,
            MaterialId = material.Id,
            EstimatedQuantity = request.EstimatedQuantity,
            // Sao chép đơn giá tại thời điểm chọn — xem ConstructionMaterial.UnitPrice.
            UnitPrice = material.UnitPrice,
            Note = request.Note,
            CreatedBy = accountId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _unitOfWork.GetRepository<ConstructionMaterial>().InsertAsync(usage);
        await _unitOfWork.CommitAsync();

        usage.Material = material;
        return ConstructionMaterialResponse.From(usage);
    }

    public async Task<ConstructionMaterialResponse> UpdateUsageAsync(
        Guid accountId, Guid id, UpdateConstructionMaterialRequest request)
    {
        var usage = await LoadUsageAsync(id);
        var engagementId = usage.ConstructionItemId != null
            ? usage.ConstructionItem!.ProjectWorkingId
            : usage.ConstructionTask!.ConstructionItem.ProjectWorkingId;

        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagementId),
            "edit materials of a construction item", EngagementActor.Provider);

        if (request.EstimatedQuantity.HasValue)
        {
            if (request.EstimatedQuantity.Value <= 0)
                throw new ArgumentException("The estimated quantity must be greater than 0.");
            usage.EstimatedQuantity = request.EstimatedQuantity.Value;
        }

        if (request.ActualQuantity.HasValue)
        {
            if (request.ActualQuantity.Value < 0)
                throw new ArgumentException("The actual quantity cannot be negative.");

            // Lượng THỰC TẾ là con số ghi nhận sau khi đã làm (review 3). Công việc còn 'pending'
            // thì chưa động tới vật tư nào, ghi số thực tế lúc đó chỉ là dự tính đội lốt.
            var status = usage.ConstructionItemId != null
                ? usage.ConstructionItem!.Status
                : usage.ConstructionTask!.Status;
            if (status == ItemStatus.pending)
                throw new InvalidOperationException(
                    "The work has not started ('pending') — actual material quantities can only be recorded once construction is under way.");

            usage.ActualQuantity = request.ActualQuantity.Value;
        }

        if (request.Note != null) usage.Note = request.Note;

        usage.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.GetRepository<ConstructionMaterial>().Update(usage);
        await _unitOfWork.CommitAsync();

        return ConstructionMaterialResponse.From(usage);
    }

    public async Task RemoveUsageAsync(Guid accountId, Guid id)
    {
        var usage = await LoadUsageAsync(id);
        var engagementId = usage.ConstructionItemId != null
            ? usage.ConstructionItem!.ProjectWorkingId
            : usage.ConstructionTask!.ConstructionItem.ProjectWorkingId;

        EngagementAuthorization.EnsureActor(
            await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagementId),
            "remove materials from a construction item", EngagementActor.Provider);

        _unitOfWork.GetRepository<ConstructionMaterial>().Delete(usage);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Gộp chi phí vật tư của milestone: phần khai thẳng ở milestone + phần của MỌI task con.
    /// Tổng thực tế chỉ trả về khi không còn dòng nào thiếu lượng thực tế — cộng một nửa số liệu
    /// rồi gọi đó là "chi phí thực tế" thì con số đó sai mà nhìn vẫn như đúng.
    /// </summary>
    public async Task<MaterialCostSummaryResponse> GetItemCostAsync(Guid accountId, Guid constructionItemId)
    {
        var item = await LoadItemAsync(constructionItemId);
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, item.ProjectWorkingId);

        var taskIds = await _unitOfWork.GetRepository<ConstructionTask>()
            .GetListAsync(selector: t => t.Id, predicate: t => t.ConstructionItemId == constructionItemId);

        var usages = await _unitOfWork.GetRepository<ConstructionMaterial>().GetListAsync(
            predicate: u => u.ConstructionItemId == constructionItemId
                            || (u.ConstructionTaskId != null && taskIds.Contains(u.ConstructionTaskId.Value)),
            include: q => q.Include(u => u.Material),
            orderBy: q => q.OrderBy(u => u.CreatedAt));

        var own = usages.Where(u => u.ConstructionItemId != null).ToList();
        var fromTasks = usages.Where(u => u.ConstructionTaskId != null).ToList();

        decimal Estimated(IEnumerable<ConstructionMaterial> xs) => xs.Sum(u => u.EstimatedQuantity * u.UnitPrice);
        decimal? Actual(IEnumerable<ConstructionMaterial> xs)
        {
            var list = xs.ToList();
            if (list.Count == 0) return 0m;
            return list.Any(u => u.ActualQuantity == null)
                ? null
                : list.Sum(u => u.ActualQuantity!.Value * u.UnitPrice);
        }

        var ownActual = Actual(own);
        var taskActual = Actual(fromTasks);

        return new MaterialCostSummaryResponse
        {
            ConstructionItemId = constructionItemId,
            OwnEstimatedCost = Estimated(own),
            OwnActualCost = ownActual,
            TasksEstimatedCost = Estimated(fromTasks),
            TasksActualCost = taskActual,
            TotalEstimatedCost = Estimated(usages),
            TotalActualCost = ownActual != null && taskActual != null ? ownActual + taskActual : null,
            MissingActualCount = usages.Count(u => u.ActualQuantity == null),
            Lines = usages.Select(ConstructionMaterialResponse.From).ToList()
        };
    }

    // ───────────────────────── Helper ─────────────────────────

    private static MaterialUnit ParseUnit(string unit)
    {
        if (!Enum.TryParse<MaterialUnit>(unit?.Trim(), ignoreCase: true, out var parsed))
            throw new ArgumentException(
                $"Unit '{unit}' is not valid. Allowed: {string.Join(", ", Enum.GetNames<MaterialUnit>())}.");
        return parsed;
    }

    private async Task<Material> LoadAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: m => m.Id == id)
        ?? throw new KeyNotFoundException($"No material found with id {id}.");

    private async Task<ConstructionItem> LoadItemAsync(Guid id) =>
        await _unitOfWork.GetRepository<ConstructionItem>()
            .SingleOrDefaultAsync(predicate: i => i.Id == id)
        ?? throw new KeyNotFoundException($"No construction item found with id {id}.");

    private async Task<ConstructionTask> LoadTaskAsync(Guid id) =>
        await _unitOfWork.GetRepository<ConstructionTask>()
            .SingleOrDefaultAsync(
                predicate: t => t.Id == id,
                include: q => q.Include(t => t.ConstructionItem))
        ?? throw new KeyNotFoundException($"No construction task found with id {id}.");

    private async Task<ConstructionMaterial> LoadUsageAsync(Guid id) =>
        await _unitOfWork.GetRepository<ConstructionMaterial>()
            .SingleOrDefaultAsync(
                predicate: u => u.Id == id,
                include: q => q.Include(u => u.Material)
                               .Include(u => u.ConstructionItem!)
                               .Include(u => u.ConstructionTask!).ThenInclude(t => t.ConstructionItem))
        ?? throw new KeyNotFoundException($"No material line found with id {id}.");

    private async Task<ProjectWorking> LoadEngagementAsync(Guid id) =>
        await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == id)
        ?? throw new KeyNotFoundException($"No project provider found with id {id}.");

    private async Task EnsureSignedContractAsync(Guid engagementId)
    {
        var signed = await _unitOfWork.GetRepository<Contract>()
            .CountAsync(c => c.ProjectWorkingId == engagementId && c.Status == ContractStatus.confirmed) > 0;
        if (!signed)
            throw new InvalidOperationException(
                "The engagement has no signed contract — the material price list cannot be published yet.");
    }
}
