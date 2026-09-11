using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTemplate;
using SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTemplate;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Mẫu quy trình thi công tái dùng (review 3: "add thêm template cho quá trình thi công").
///
/// Áp mẫu là COPY một lần sang <c>construction_items</c> + <c>construction_tasks</c>, không tạo
/// liên kết sống: sửa mẫu về sau không được phép làm xê dịch kế hoạch của dự án đang chạy.
/// Mốc <c>estimate_at</c> được giãn dần theo <c>EstimateDays</c> của từng hạng mục, tính từ ngày
/// bắt đầu do provider truyền vào.
/// </summary>
public class ConstructionTemplateService : IConstructionTemplateService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<ConstructionTemplate> _repository;

    public ConstructionTemplateService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<ConstructionTemplate>();
    }

    public async Task<PaginationResponse<ConstructionTemplateResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10, string? serviceKind = null)
    {
        ServiceKind? kind = null;
        if (!string.IsNullOrWhiteSpace(serviceKind))
        {
            if (!Enum.TryParse<ServiceKind>(serviceKind.Trim(), ignoreCase: true, out var parsed))
                throw new ArgumentException(
                    $"ServiceKind '{serviceKind}' is not valid. Allowed: design, construction, both.");
            kind = parsed;
        }

        // Thấy mẫu công khai + mẫu của chính mình. Mẫu riêng của provider khác là bí quyết nghề của
        // họ, không phơi ra cho đối thủ trên cùng sàn.
        var query = _repository
            .GetQueryable(
                t => (kind == null || t.ServiceKind == kind)
                     && (t.IsPublic || t.CreatedBy == accountId),
                include: BuildInclude())
            .OrderByDescending(t => t.IsPublic).ThenByDescending(t => t.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ConstructionTemplateResponse>(
            paged.Items.Select(ConstructionTemplateResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ConstructionTemplateResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var template = await LoadAsync(id);
        EnsureVisible(template, accountId);
        return ConstructionTemplateResponse.From(template);
    }

    public async Task<ConstructionTemplateResponse> CreateAsync(
        Guid accountId, CreateConstructionTemplateRequest request)
    {
        if (request.Items.Count == 0)
            throw new ArgumentException("A template must have at least one item.");

        if (!Enum.TryParse<ServiceKind>(request.ServiceKind?.Trim() ?? "construction", ignoreCase: true, out var kind))
            throw new ArgumentException(
                $"ServiceKind '{request.ServiceKind}' is not valid. Allowed: design, construction, both.");

        var now = DateTime.UtcNow;
        var template = new ConstructionTemplate
        {
            Name = request.Name,
            Description = request.Description,
            ServiceKind = kind,
            // Mẫu công khai là quyết định quản trị — provider tự đánh dấu thì sàn loạn ngay.
            IsPublic = false,
            CreatedBy = accountId,
            CreatedAt = now,
            UpdatedAt = now,
            Items = BuildItems(request.Items)
        };

        await _repository.InsertAsync(template);
        await _unitOfWork.CommitAsync();

        return ConstructionTemplateResponse.From(await LoadAsync(template.Id));
    }

    public async Task<ConstructionTemplateResponse> ReorderItemsAsync(
        Guid accountId, Guid id, ReorderConstructionTemplateItemsRequest request)
    {
        var template = await LoadAsync(id);

        if (template.CreatedBy != accountId && !await IsAdminAsync(accountId))
            throw new UnauthorizedAccessException(
                "Only the template's author (or an admin) may reorder this template's items.");

        if (request.ItemIds.Count == 0)
            throw new ArgumentException("itemIds is required — send every item of the template in the order you want.");

        if (request.ItemIds.Distinct().Count() != request.ItemIds.Count)
            throw new ArgumentException("itemIds lists the same template item more than once.");

        var byId = template.Items.ToDictionary(i => i.Id);

        var unknown = request.ItemIds.Where(itemId => !byId.ContainsKey(itemId)).ToList();
        if (unknown.Count > 0)
            throw new InvalidOperationException(
                $"{unknown.Count} of the items sent do not belong to this template.");

        var missing = byId.Keys.Where(itemId => !request.ItemIds.Contains(itemId)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Send the whole template: {missing.Count} item(s) are missing from itemIds.");

        // Update() tường minh cho TỪNG hạng mục: mọi truy vấn đọc của GenericRepository đều
        // AsNoTracking, nên gán thẳng vào entity vừa nạp chỉ đổi bộ nhớ — CommitAsync không có gì
        // để lưu và API vẫn trả về đúng thứ tự mới (nó dựng từ object trong bộ nhớ) trong khi DB
        // giữ nguyên thứ tự cũ.
        var itemRepository = _unitOfWork.GetRepository<ConstructionTemplateItem>();
        for (var index = 0; index < request.ItemIds.Count; index++)
        {
            var item = byId[request.ItemIds[index]];
            var next = index + 1;
            if (item.SortOrder == next) continue;
            item.SortOrder = next;
            itemRepository.Update(item);
        }

        await _unitOfWork.CommitAsync();

        return ConstructionTemplateResponse.From(template);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var template = await LoadAsync(id);

        if (template.CreatedBy != accountId && !await IsAdminAsync(accountId))
            throw new UnauthorizedAccessException("Only the template's author (or an admin) may delete this template.");

        _repository.Delete(template);
        await _unitOfWork.CommitAsync();
    }

    /// <summary>
    /// Áp mẫu vào một engagement: sinh <c>construction_item</c> cho từng hạng mục và
    /// <c>construction_task</c> cho từng việc con, mốc thời gian giãn dần theo EstimateDays.
    /// </summary>
    public async Task<ApplyTemplateResponse> ApplyAsync(
        Guid accountId, Guid id, ApplyConstructionTemplateRequest request)
    {
        var template = await LoadAsync(id);
        EnsureVisible(template, accountId);

        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(
                predicate: e => e.Id == request.ProjectWorkingId,
                include: q => q.Include(e => e.ServiceProviderProfile))
            ?? throw new KeyNotFoundException(
                $"No project provider found with id {request.ProjectWorkingId}.");

        if (engagement.ServiceProviderProfile.AccountId != accountId && !await IsAdminAsync(accountId))
            throw new UnauthorizedAccessException(
                "Only the provider of this engagement may apply a process template to the project.");

        // Cùng guard với tạo hạng mục thủ công: phải có hợp đồng đã ký mới được lập kế hoạch thi công.
        var signed = await _unitOfWork.GetRepository<Contract>().CountAsync(
            c => c.ProjectWorkingId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
        if (!signed)
            throw new InvalidOperationException(
                "The engagement has no signed contract — a construction process template cannot be applied yet.");

        var now = DateTime.UtcNow;
        var cursor = request.StartDate ?? VietnamTime.Today;

        // Mọi estimate_at sinh ra đều đếm tiến từ mốc này, nên mốc lùi về quá khứ là cả cây kế
        // hoạch nằm trong quá khứ — đúng thứ mà luồng tạo hạng mục thủ công đã chặn. Không guard
        // ở đây thì áp mẫu trở thành đường vòng qua ConstructionSchedule.
        ConstructionSchedule.EnsureEstimateNotInPast(cursor, "the start date for applying the process template");

        var items = new List<ConstructionItem>();
        var tasks = new List<ConstructionTask>();

        // Thứ tự của mẫu phải đi theo sang dự án. Không chép sang SortOrder thì mọi hạng mục sinh
        // ra đều mang 0, và danh sách kế hoạch lại rơi về sắp theo ngày — đúng cái mà cột SortOrder
        // sinh ra để thay thế.
        //
        // Đếm TIẾP từ hạng mục cuối cùng chứ không bắt đầu lại từ 1: áp mẫu là NỐI THÊM vào kế
        // hoạch đang có (xem cảnh báo "appendWarning" trên FE). Bắt đầu lại từ 1 thì cả bộ hạng mục
        // mới trùng số thứ tự với bộ đang chạy, khoá sắp xếp hoà nhau và hai bộ cài răng lược vào
        // nhau theo mốc thời gian.
        var existingOrders = await _unitOfWork.GetRepository<ConstructionItem>().GetListAsync(
            selector: i => i.SortOrder,
            predicate: i => i.ProjectWorkingId == engagement.Id && i.ParentId == null);
        var itemSortOrder = existingOrders.Count == 0 ? 1 : existingOrders.Max() + 1;

        foreach (var templateItem in template.Items.OrderBy(i => i.SortOrder))
        {
            // EstimateDays là SỐ NGÀY hạng mục chiếm, còn ConstructionSchedule.DurationDays đếm
            // cả hai đầu (e − s + 1). Nên hạng mục n ngày chạy từ cursor đến cursor + n − 1, và
            // hạng mục kế tiếp bắt đầu NGÀY HÔM SAU.
            //
            // Trước đây cộng thẳng n rồi lấy luôn mốc đó làm ngày bắt đầu của hạng mục sau, tức
            // đếm theo ngày TRÔI QUA trong khi phần còn lại của hệ đếm theo ngày BAO GỒM. Hệ quả:
            // mẫu khai 71 ngày sinh ra kế hoạch dài 72 ngày, và hai hạng mục liền nhau cùng nhận
            // một ngày làm mốc — "Phần thô" bắt đầu đúng hôm "Chuẩn bị mặt bằng" kết thúc.
            //
            // n = 0 thì hạng mục không chiếm ngày nào: mốc đầu trùng mốc cuối và cursor đứng yên.
            var itemDays = templateItem.EstimateDays ?? 0;
            var itemStart = cursor;
            var itemEnd = itemDays > 0 ? cursor.AddDays(itemDays - 1) : cursor;

            var item = new ConstructionItem
            {
                ProjectWorkingId = engagement.Id,
                SortOrder = itemSortOrder++,
                Name = templateItem.Name,
                Description = templateItem.Description,
                Category = templateItem.Category,
                // Ghi CẢ HAI đầu mốc. itemStart vốn đã tính sẵn ở trên nhưng trước đây bị bỏ đi,
                // nên hạng mục sinh từ mẫu có hạn hoàn thành mà không có ngày bắt đầu:
                // PlannedDurationDays (StartAt → EstimateAt) luôn null và FE không dựng được
                // khoảng thời gian thật, phải lấy tạm created_at làm ngày bắt đầu.
                StartAt = itemStart,
                EstimateAt = itemEnd,
                Status = ItemStatus.pending,
                // Vết nguồn: hạng mục này ra đời từ mẫu nào. Chép xong mà không ghi lại thì phía
                // chủ quán không có cách nào biết nhà thầu đang chạy theo quy trình gì — kế hoạch
                // trông y hệt như gõ tay. Vẫn là copy một lần: sửa mẫu về sau không đụng vào đây.
                SourceTemplateId = template.Id,
                CreatedBy = accountId,
                CreatedAt = now,
                UpdatedAt = now
            };
            items.Add(item);

            var taskCursor = itemStart;
            foreach (var templateTask in templateItem.Tasks.OrderBy(t => t.SortOrder))
            {
                // Cùng cách đếm với hạng mục, để tổng các việc con vừa khít khoảng của hạng mục
                // cha thay vì tràn ra ngoài.
                var taskDays = templateTask.EstimateDays ?? 0;
                var taskStart = taskCursor;
                var taskEnd = taskDays > 0 ? taskCursor.AddDays(taskDays - 1) : taskCursor;
                tasks.Add(new ConstructionTask
                {
                    // FK gán qua navigation: khoá chính của item chỉ có thật SAU CommitAsync
                    // (uuid do DB sinh), gán ConstructionItemId lúc này sẽ ra Guid.Empty.
                    ConstructionItem = item,
                    Name = templateTask.Name,
                    Description = templateTask.Description,
                    StartAt = taskStart,
                    EstimateAt = taskEnd,
                    Status = ItemStatus.pending,
                    CreatedBy = accountId,
                    CreatedAt = now,
                    UpdatedAt = now
                });

                if (taskDays > 0) taskCursor = taskEnd.AddDays(1);
            }

            if (itemDays > 0) cursor = itemEnd.AddDays(1);
        }

        await _unitOfWork.GetRepository<ConstructionItem>().InsertRangeAsync(items);
        await _unitOfWork.GetRepository<ConstructionTask>().InsertRangeAsync(tasks);
        await _unitOfWork.CommitAsync();

        return new ApplyTemplateResponse
        {
            ConstructionTemplateId = template.Id,
            ProjectWorkingId = engagement.Id,
            CreatedItems = items.Count,
            CreatedTasks = tasks.Count,
            // Ngày LÀM VIỆC cuối cùng của kế hoạch, không phải cursor: cursor đã nhảy sang hôm
            // sau để hạng mục kế tiếp có chỗ bắt đầu, nên trả về nó là báo thừa một ngày.
            PlannedFinishAt = items.Count == 0
                ? cursor
                : items.Max(i => i.EstimateAt ?? cursor)
        };
    }

    /// <summary>
    /// Đọc NGƯỢC vết nguồn: những mẫu nào đã được áp vào engagement này, và mỗi mẫu phủ những
    /// hạng mục nào của kế hoạch.
    ///
    /// Gom theo <c>SourceTemplateId</c> chứ không trả về một mẫu duy nhất, vì áp mẫu là NỐI THÊM —
    /// nhà thầu áp mẫu "Phần thô" rồi áp tiếp "Hoàn thiện" là chuyện bình thường, và chủ quán cần
    /// thấy đủ cả hai.
    ///
    /// Mẫu đã bị xoá thì FK về null nên rơi khỏi danh sách, còn các hạng mục nó sinh ra vẫn nằm
    /// nguyên trong kế hoạch (SetNull) — mất tên quy trình, không mất tiến độ.
    /// </summary>
    public async Task<List<AppliedConstructionTemplateResponse>> GetAppliedAsync(
        Guid accountId, Guid projectWorkingId)
    {
        // KHÔNG dùng EnsureVisible ở đây: luật public/của-chính-mình là luật của THƯ VIỆN mẫu.
        // Ở màn dự án, thứ quyết định quyền xem là "có phải một bên của engagement không" —
        // nếu không thì chủ quán chẳng bao giờ nhìn được mẫu riêng mà nhà thầu vừa áp cho mình.
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, projectWorkingId);

        // Chỉ hạng mục GỐC (ParentId = null): mẫu sinh ra hạng mục cấp một, việc con nằm ở
        // construction_tasks. Đếm cả cấp con sẽ thổi phồng AppliedItemCount.
        var items = await _unitOfWork.GetRepository<ConstructionItem>().GetListAsync(
            predicate: i => i.ProjectWorkingId == projectWorkingId
                            && i.ParentId == null
                            && i.SourceTemplateId != null,
            include: q => q.Include(i => i.SourceTemplate!));

        return [.. items
            .GroupBy(i => i.SourceTemplateId!.Value)
            .Select(group =>
            {
                var ordered = group.OrderBy(i => i.SortOrder).ThenBy(i => i.CreatedAt).ToList();
                var template = ordered[0].SourceTemplate!;

                return new AppliedConstructionTemplateResponse
                {
                    ConstructionTemplateId = template.Id,
                    ProjectWorkingId = projectWorkingId,
                    Name = template.Name,
                    Description = template.Description,
                    ServiceKind = template.ServiceKind.ToString(),
                    IsPublic = template.IsPublic,
                    AppliedItemCount = ordered.Count,
                    CompletedItemCount = ordered.Count(i => i.Status == ItemStatus.completed),
                    // Mốc áp mẫu = lúc lứa hạng mục đó được tạo; ApplyAsync đặt cùng một `now`
                    // cho cả lứa nên Min ở đây là chính mốc bấm nút.
                    AppliedAt = ordered.Min(i => i.CreatedAt),
                    // Mốc kế hoạch đọc từ hạng mục THẬT, không đọc từ EstimateDays của mẫu: sau khi
                    // áp, nhà thầu còn kéo lịch, nên con số của mẫu không còn đúng với dự án nữa.
                    PlannedStartAt = ordered.Where(i => i.StartAt != null).Select(i => i.StartAt).Min(),
                    PlannedFinishAt = ordered.Where(i => i.EstimateAt != null).Select(i => i.EstimateAt).Max(),
                    ItemNames = [.. ordered.Select(i => i.Name)]
                };
            })
            .OrderBy(r => r.AppliedAt)];
    }

    // ───────────────────────── Helper ─────────────────────────

    private static List<ConstructionTemplateItem> BuildItems(List<ConstructionTemplateItemInput> inputs)
    {
        var sortOrder = 0;
        return inputs.Select(input =>
        {
            if (string.IsNullOrWhiteSpace(input.Name))
                throw new ArgumentException("Every item in the template must have a name.");
            if (input.EstimateDays is < 0)
                throw new ArgumentException($"The duration of item '{input.Name}' cannot be negative.");

            var taskOrder = 0;
            return new ConstructionTemplateItem
            {
                Name = input.Name,
                Description = input.Description,
                Category = input.Category,
                EstimateDays = input.EstimateDays,
                SortOrder = sortOrder++,
                Tasks = input.Tasks.Select(task => new ConstructionTemplateTask
                {
                    Name = !string.IsNullOrWhiteSpace(task.Name)
                        ? task.Name
                        : throw new ArgumentException("Every sub-task in the template must have a name."),
                    Description = task.Description,
                    EstimateDays = task.EstimateDays,
                    SortOrder = taskOrder++
                }).ToList()
            };
        }).ToList();
    }

    private static void EnsureVisible(ConstructionTemplate template, Guid accountId)
    {
        if (template.IsPublic || template.CreatedBy == accountId) return;

        throw new UnauthorizedAccessException("This template is private to another provider.");
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    private static Func<IQueryable<ConstructionTemplate>, IIncludableQueryable<ConstructionTemplate, object>> BuildInclude() =>
        q => q.Include(t => t.Items).ThenInclude(i => i.Tasks);

    private async Task<ConstructionTemplate> LoadAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: t => t.Id == id, include: BuildInclude())
        ?? throw new KeyNotFoundException($"No process template found with id {id}.");
}
