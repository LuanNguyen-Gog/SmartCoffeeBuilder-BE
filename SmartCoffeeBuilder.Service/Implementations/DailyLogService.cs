using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.DailyLog;
using SmartCoffeeBuilder.Service.DTOs.Responses.DailyLog;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Nhật ký thi công hằng ngày (review 3). Quyền xét theo engagement như
/// <see cref="ConstructionItemService"/>: GHI chỉ nhà cung cấp, ĐỌC cả hai bên.
/// </summary>
public class DailyLogService : IDailyLogService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<DailyLog> _repository;
    private readonly IFileStorageService _fileStorage;

    public DailyLogService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<DailyLog>();
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<DailyLogResponse>> GetAllAsync(
        Guid accountId,
        int pageNumber = 1, int pageSize = 20,
        Guid? projectWorkingId = null,
        Guid? constructionItemId = null,
        Guid? constructionTaskId = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null)
    {
        if (fromDate is DateOnly f && toDate is DateOnly t && f > t)
            throw new ArgumentException(
                $"fromDate '{f:yyyy-MM-dd}' nằm sau toDate '{t:yyyy-MM-dd}'.");

        // Lọc TRONG query (null = admin, xem tất cả) — lọc sau khi lấy về sẽ làm sai TotalItems.
        var visibleEngagementIds = await EngagementAuthorization
            .GetVisibleEngagementIdsAsync(_unitOfWork, accountId);

        var query = _repository
            .GetQueryable(
                e => (projectWorkingId == null || e.ProjectWorkingId == projectWorkingId)
                     && (constructionItemId == null || e.ConstructionItemId == constructionItemId)
                     && (constructionTaskId == null || e.ConstructionTaskId == constructionTaskId)
                     && (fromDate == null || e.LogDate >= fromDate)
                     && (toDate == null || e.LogDate <= toDate)
                     && (visibleEngagementIds == null
                         || visibleEngagementIds.Contains(e.ProjectWorkingId)),
                include: q => q
                    .Include(e => e.ConstructionItem)
                    .Include(e => e.ConstructionTask)
                    .Include(e => e.Media)
                    .Include(e => e.CreatedByAccount!).ThenInclude(a => a!.ServiceProviderProfile)
                    .Include(e => e.CreatedByAccount!).ThenInclude(a => a!.ShopOwner))
            // Nhật ký đọc theo dòng thời gian ngược: hôm nay trước. ThenBy CreatedAt cho hai bản
            // ghi cùng ngày (ghi bù nhiều buổi), ThenBy Id để phân trang ổn định.
            .OrderByDescending(e => e.LogDate)
            .ThenByDescending(e => e.CreatedAt)
            .ThenBy(e => e.Id);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<DailyLogResponse>(
            paged.Items.Select(DailyLogResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<DailyLogResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var log = await LoadForActionAsync(accountId, id, "xem nhật ký thi công",
            EngagementActor.Owner, EngagementActor.Provider);

        return DailyLogResponse.From(log);
    }

    public async Task<DailyLogResponse> CreateAsync(Guid accountId, CreateDailyLogRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WorkDone))
            throw new ArgumentException("WorkDone không được để trống — nhật ký trống không có giá trị.");

        var anchor = await ResolveAnchorAsync(
            request.ProjectWorkingId, request.ConstructionItemId, request.ConstructionTaskId);

        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, anchor.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, "ghi nhật ký thi công", EngagementActor.Provider);

        var logDate = request.LogDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        EnsureLogDateNotInFuture(logDate);

        var log = new DailyLog
        {
            ProjectWorkingId = anchor.ProjectWorkingId,
            ConstructionItemId = anchor.ConstructionItemId,
            ConstructionTaskId = anchor.ConstructionTaskId,
            LogDate = logDate,
            WorkDone = request.WorkDone.Trim(),
            IssueNote = Clean(request.IssueNote),
            WeatherNote = Clean(request.WeatherNote),
            WorkerCount = EnsureWorkerCountValid(request.WorkerCount),
            CreatedBy = accountId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(log);
        await _unitOfWork.CommitAsync(); // uuid sinh ở DB — phải commit mới có log.Id cho bảng con

        if (request.Media is { Count: > 0 })
        {
            await InsertMediaAsync(log.Id, request.Media);
            await _unitOfWork.CommitAsync();
        }

        return await GetByIdAsync(accountId, log.Id);
    }

    public async Task<DailyLogResponse> UpdateAsync(Guid accountId, Guid id, UpdateDailyLogRequest request)
    {
        var log = await LoadForActionAsync(accountId, id, "sửa nhật ký thi công", EngagementActor.Provider);

        // Đổi chỗ neo thì phải neo lại trong CÙNG engagement — không cho chuyển nhật ký sang dự án khác.
        if (request.ConstructionItemId != null || request.ConstructionTaskId != null)
        {
            var anchor = await ResolveAnchorAsync(
                log.ProjectWorkingId,
                request.ConstructionItemId ?? log.ConstructionItemId,
                request.ConstructionTaskId ?? log.ConstructionTaskId);

            log.ConstructionItemId = anchor.ConstructionItemId;
            log.ConstructionTaskId = anchor.ConstructionTaskId;
        }

        if (request.LogDate is DateOnly date)
        {
            EnsureLogDateNotInFuture(date);
            log.LogDate = date;
        }

        if (request.WorkDone != null)
        {
            if (string.IsNullOrWhiteSpace(request.WorkDone))
                throw new ArgumentException("WorkDone không được để trống.");
            log.WorkDone = request.WorkDone.Trim();
        }

        if (request.IssueNote != null) log.IssueNote = Clean(request.IssueNote);
        if (request.WeatherNote != null) log.WeatherNote = Clean(request.WeatherNote);
        if (request.WorkerCount != null) log.WorkerCount = EnsureWorkerCountValid(request.WorkerCount);

        // Media khác null = THAY TOÀN BỘ danh sách (mảng rỗng để gỡ hết).
        List<string> removedFiles = [];
        if (request.Media != null)
        {
            var mediaRepo = _unitOfWork.GetRepository<DailyLogMedia>();
            var existing = await mediaRepo.GetListAsync(predicate: m => m.DailyLogId == log.Id);

            // Dựng hàng mới TRƯỚC rồi mới so, vì so phải diễn ra trên giá trị ĐÃ CHUẨN HOÁ:
            // FE thường gửi lại nguyên URL public, còn DB lưu ObjectName. So thô hai chuỗi đó
            // sẽ coi file đang được giữ là "đã gỡ" và xoá mất nó trên bucket.
            var newRows = await BuildMediaAsync(log.Id, request.Media);
            var keptUrls = newRows.Select(m => m.MediaUrl).ToHashSet();
            removedFiles = [.. existing.Where(m => !keptUrls.Contains(m.MediaUrl)).Select(m => m.MediaUrl)];

            mediaRepo.DeleteRange(existing);
            if (newRows.Count > 0) await mediaRepo.InsertRangeAsync(newRows);
        }

        log.UpdatedAt = DateTime.UtcNow;
        _repository.Update(log);
        await _unitOfWork.CommitAsync();

        // Dọn file SAU khi DB commit — best-effort, lỗi ở đây không làm hỏng nghiệp vụ.
        foreach (var file in removedFiles) await _fileStorage.TryDeleteAsync(file);

        return await GetByIdAsync(accountId, log.Id);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var log = await LoadForActionAsync(accountId, id, "xoá nhật ký thi công", EngagementActor.Provider);

        var files = log.Media.Select(m => m.MediaUrl).ToList();

        _repository.Delete(log); // daily_log_media cascade theo DB
        await _unitOfWork.CommitAsync();

        foreach (var file in files) await _fileStorage.TryDeleteAsync(file);
    }

    // ───────────────────────── Nội bộ ─────────────────────────

    /// <summary>Chỗ neo đã kiểm tra tính nhất quán: task ⊂ hạng mục ⊂ engagement.</summary>
    private sealed record DailyLogAnchor(
        Guid ProjectWorkingId, Guid? ConstructionItemId, Guid? ConstructionTaskId);

    /// <summary>
    /// Suy ra engagement từ task/hạng mục nếu caller không truyền, và chặn trường hợp ba id trỏ về
    /// ba chỗ khác nhau — nếu không thì nhật ký lọt sang engagement mà người ghi không có quyền.
    /// Gắn task mà bỏ trống hạng mục thì tự điền hạng mục cha.
    /// </summary>
    private async Task<DailyLogAnchor> ResolveAnchorAsync(
        Guid? projectWorkingId, Guid? constructionItemId, Guid? constructionTaskId)
    {
        Guid? resolvedItemId = constructionItemId;
        Guid? engagementFromChain = null;

        if (constructionTaskId is Guid taskId)
        {
            var task = await _unitOfWork.GetRepository<ConstructionTask>()
                .SingleOrDefaultAsync(
                    predicate: t => t.Id == taskId,
                    include: q => q.Include(t => t.ConstructionItem))
                ?? throw new KeyNotFoundException($"Không tìm thấy task thi công với id {taskId}.");

            if (constructionItemId is Guid itemId && task.ConstructionItemId != itemId)
                throw new ArgumentException(
                    $"Task {taskId} không thuộc hạng mục {itemId} — kiểm tra lại constructionItemId.");

            resolvedItemId = task.ConstructionItemId;
            engagementFromChain = task.ConstructionItem.ProjectWorkingId;
        }
        else if (constructionItemId is Guid itemId)
        {
            var item = await _unitOfWork.GetRepository<ConstructionItem>()
                .SingleOrDefaultAsync(predicate: ci => ci.Id == itemId)
                ?? throw new KeyNotFoundException($"Không tìm thấy hạng mục thi công với id {itemId}.");

            engagementFromChain = item.ProjectWorkingId;
        }

        if (projectWorkingId is Guid given && engagementFromChain is Guid derived && given != derived)
            throw new ArgumentException(
                "Hạng mục/task được chọn không thuộc engagement đã truyền — nhật ký phải nằm cùng một hợp tác.");

        var finalEngagementId = engagementFromChain ?? projectWorkingId
            ?? throw new ArgumentException(
                "Thiếu chỗ neo: truyền projectWorkingId, hoặc constructionItemId / constructionTaskId để suy ra.");

        return new DailyLogAnchor(finalEngagementId, resolvedItemId, constructionTaskId);
    }

    /// <summary>
    /// Dựng các dòng media đã CHUẨN HOÁ (URL public → ObjectName, kiểm tra file có thật trên
    /// bucket) nhưng CHƯA insert — tách ra để `UpdateAsync` so được danh sách cũ/mới trên cùng
    /// một dạng giá trị trước khi quyết định xoá file nào.
    /// </summary>
    private async Task<List<DailyLogMedia>> BuildMediaAsync(
        Guid dailyLogId, List<DailyLogMediaRequest> media)
    {
        var rows = new List<DailyLogMedia>(media.Count);

        for (var i = 0; i < media.Count; i++)
        {
            var m = media[i];
            if (string.IsNullOrWhiteSpace(m.MediaUrl))
                throw new ArgumentException($"Media[{i}].MediaUrl không được để trống.");

            // Chuẩn hoá về ObjectName + kiểm tra file có thật, giống các entity lưu chuỗi file khác.
            var stored = await _fileStorage.NormalizeForStorageAsync(m.MediaUrl, $"Media[{i}].MediaUrl");

            rows.Add(new DailyLogMedia
            {
                DailyLogId = dailyLogId,
                MediaUrl = stored!,
                MediaType = ParseMediaType(m.MediaType, i),
                Caption = Clean(m.Caption),
                SortOrder = i,
                CreatedAt = DateTime.UtcNow
            });
        }

        return rows;
    }

    private async Task InsertMediaAsync(Guid dailyLogId, List<DailyLogMediaRequest> media)
    {
        var rows = await BuildMediaAsync(dailyLogId, media);
        await _unitOfWork.GetRepository<DailyLogMedia>().InsertRangeAsync(rows);
    }

    private static DailyLogMediaType ParseMediaType(string? raw, int index)
    {
        if (string.IsNullOrWhiteSpace(raw)) return DailyLogMediaType.image;

        if (!Enum.TryParse<DailyLogMediaType>(raw.Trim(), ignoreCase: true, out var parsed))
            throw new ArgumentException(
                $"Media[{index}].MediaType '{raw}' không hợp lệ. Cho phép: image, video.");

        return parsed;
    }

    /// <summary>
    /// Nhật ký ghi lại việc ĐÃ làm — ngày tương lai là dữ liệu sai, không phải kế hoạch.
    /// Mốc lấy theo UTC cho khớp phần còn lại của tầng service; VN là UTC+7 nên mốc này chỉ có thể
    /// DỄ hơn giờ địa phương, không bao giờ chặn nhầm một ngày hợp lệ.
    /// </summary>
    private static void EnsureLogDateNotInFuture(DateOnly logDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (logDate > today)
            throw new ArgumentException(
                $"LogDate '{logDate:yyyy-MM-dd}' nằm sau ngày hiện tại ({today:yyyy-MM-dd}) — " +
                "nhật ký chỉ ghi việc đã làm.");
    }

    private static int? EnsureWorkerCountValid(int? workerCount)
    {
        if (workerCount is int count && count < 0)
            throw new ArgumentException("WorkerCount không được âm.");
        return workerCount;
    }

    /// <summary>Chuỗi rỗng/khoảng trắng từ FE quy về null để không lưu ô trắng vào DB.</summary>
    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Nạp nhật ký và chốt quyền trong một bước — mọi endpoint theo id đều đi qua đây.</summary>
    private async Task<DailyLog> LoadForActionAsync(
        Guid accountId, Guid id, string action, params EngagementActor[] allowed)
    {
        var log = await _repository.SingleOrDefaultAsync(
            predicate: e => e.Id == id,
            include: q => q
                .Include(e => e.ConstructionItem)
                .Include(e => e.ConstructionTask)
                .Include(e => e.Media)
                .Include(e => e.CreatedByAccount!).ThenInclude(a => a!.ServiceProviderProfile)
                .Include(e => e.CreatedByAccount!).ThenInclude(a => a!.ShopOwner))
            ?? throw new KeyNotFoundException($"Không tìm thấy nhật ký thi công với id {id}.");

        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, log.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, action, allowed);

        return log;
    }
}
