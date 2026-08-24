using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Bản thiết kế của một engagement. Quyền xét theo ENGAGEMENT
/// (<see cref="EngagementAuthorization"/>): provider làm bản vẽ, owner duyệt / yêu cầu sửa.
/// Trạng thái 'approved' là dữ liệu guard nghiệm thu tin vào, nên không để người ngoài đụng.
/// </summary>
public class DesignService : IDesignService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Design> _repository;
    private readonly IFileStorageService _fileStorage;

    public DesignService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork, IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Design>();
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<DesignResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? projectWorkingId = null, string? status = null, string? type = null)
    {
        DesignStatus? st = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DesignStatus>(status, ignoreCase: true, out var parsedStatus))
                throw new ArgumentException($"Status '{status}' is not valid.");
            st = parsedStatus;
        }

        DesignType? tp = null;
        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!Enum.TryParse<DesignType>(type, ignoreCase: true, out var parsedType))
                throw new ArgumentException($"Type '{type}' is not valid.");
            tp = parsedType;
        }

        // Lọc TRONG query (null = admin, xem tất cả) — lọc sau khi lấy về sẽ làm sai TotalItems.
        var visibleEngagementIds = await EngagementAuthorization
            .GetVisibleEngagementIdsAsync(_unitOfWork, accountId);

        var query = _repository
            .GetQueryable(
                d => (projectWorkingId == null || d.ProjectWorkingId == projectWorkingId)
                     && (st == null || d.Status == st)
                     && (tp == null || d.Type == tp)
                     && (visibleEngagementIds == null
                         || visibleEngagementIds.Contains(d.ProjectWorkingId)),
                include: q => q.Include(d => d.DesignImages))
            .OrderByDescending(d => d.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<DesignResponse>(
            paged.Items.Select(DesignResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<DesignResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var design = await LoadForActionAsync(accountId, id, "view designs",
            EngagementActor.Owner, EngagementActor.Provider);
        return DesignResponse.From(design);
    }

    public async Task<DesignResponse> CreateAsync(Guid accountId, CreateDesignRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectWorkingId)
            ?? throw new KeyNotFoundException($"No project provider found with id {request.ProjectWorkingId}.");

        // Quyền trước guard nghiệp vụ: người ngoài không dò được trạng thái engagement qua câu lỗi.
        var actor = await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, engagement.Id);
        EngagementAuthorization.EnsureActor(actor, "create a design", EngagementActor.Provider);

        if (engagement.ContractType == ServiceKind.construction)
            throw new InvalidOperationException(
                "This engagement has contract type 'construction' — it has no design phase.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"The engagement is in status '{engagement.Status}' — a design can only be created while the engagement is 'accepted'.");

        // v5: "đã ký mới được làm" — guard qua contract confirmed, không check provider_status.
        var hasConfirmedContract = await _unitOfWork.GetRepository<Contract>()
            .CountAsync(c => c.ProjectWorkingId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
        if (!hasConfirmedContract)
            throw new InvalidOperationException(
                "The engagement has no 'confirmed' contract — sign the contract before creating a design.");

        if (!Enum.TryParse<DesignType>(request.Type, ignoreCase: true, out var type))
            throw new ArgumentException(
                $"Type '{request.Type}' is not valid. Allowed: concept, layout_2d, render_3d, technical_drawing.");

        var design = new Design
        {
            ProjectWorkingId = engagement.Id,
            Title = request.Title,
            Version = 0.1m, // bản nháp đầu tiên; mỗi vòng revision +0.1
            Type = type,
            Status = DesignStatus.in_progress,
            // Người tạo lấy từ JWT, KHÔNG nhận từ body.
            CreatedBy = accountId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    public async Task<DesignResponse> UpdateAsync(Guid accountId, Guid id, UpdateDesignRequest request)
    {
        var design = await LoadForActionAsync(accountId, id, "edit a design", EngagementActor.Provider);

        if (design.Status is not (DesignStatus.in_progress or DesignStatus.revision))
            throw new InvalidOperationException(
                $"The design is in status '{design.Status}' — it can only be edited while 'in_progress' or 'revision'.");

        if (request.Title != null) design.Title = request.Title;
        if (request.Type != null)
        {
            if (!Enum.TryParse<DesignType>(request.Type, ignoreCase: true, out var type))
                throw new ArgumentException(
                    $"Type '{request.Type}' is not valid. Allowed: concept, layout_2d, render_3d, technical_drawing.");
            design.Type = type;
        }
        // Mô tả thay đổi so với bản trước — provider điền trước khi submit (review 3). Cột này bị
        // ghi đè ở vòng sau, bản lưu vĩnh viễn nằm trong snapshot design_version.
        if (request.ChangeSummary != null) design.ChangeSummary = request.ChangeSummary;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    /// <summary>
    /// Provider nộp bản design cho owner duyệt: in_progress → submitted.
    /// <paramref name="accountId"/> lấy từ JWT ở controller — ghi vào snapshot làm vết ai đã nộp.
    /// </summary>
    public async Task<DesignResponse> SubmitAsync(Guid id, Guid accountId)
    {
        var design = await LoadForActionAsync(accountId, id, "submit a design", EngagementActor.Provider);

        if (design.Status != DesignStatus.in_progress)
            throw new InvalidOperationException(
                $"Only a design that is 'in_progress' can be submitted (currently: '{design.Status}').");

        if (design.DesignImages.Count == 0)
            throw new InvalidOperationException("This design has no images yet — add images before submitting.");

        design.Status = DesignStatus.submitted;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        // Snapshot bản nộp — chạy NGOÀI transaction đổi status (best-effort, lỗi không rollback status).
        await TrySnapshotAsync(design, DesignVersionSnapshotKind.submitted, snapshottedBy: accountId);

        return DesignResponse.From(design);
    }

    /// <summary>
    /// Owner duyệt bản design: submitted → approved.
    /// Pha design "xong" là derived từ design approved — không đổi provider_status.
    /// <paramref name="accountId"/> lấy từ JWT ở controller — ghi vào snapshot làm vết ai đã duyệt.
    /// </summary>
    public async Task<DesignResponse> ApproveAsync(Guid id, Guid accountId)
    {
        var design = await LoadForActionAsync(accountId, id, "approve a design", EngagementActor.Owner);

        if (design.Status != DesignStatus.submitted)
            throw new InvalidOperationException(
                $"Only a design that is 'submitted' can be approved (currently: '{design.Status}').");

        // 'approved' là dữ liệu mà guard nghiệm thu engagement tin vào, nên checklist nghiệm thu
        // của bản vẽ phải đạt trước khi duyệt (review 3).
        await ChecklistGate.EnsureDesignPassedAsync(_unitOfWork, design.Id, "approve this design yet");

        design.Status = DesignStatus.approved;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        // Snapshot bản duyệt — chạy NGOÀI transaction đổi status. Mỗi lần approve sinh version mới
        // (lưu trữ được nhiều bản approved nếu design được duyệt nhiều lần sau revision).
        await TrySnapshotAsync(design, DesignVersionSnapshotKind.approved, snapshottedBy: accountId);

        return DesignResponse.From(design);
    }

    /// <summary>
    /// Owner yêu cầu chỉnh sửa: submitted → revision (kèm lý do).
    ///
    /// Đây cũng là chỗ ĐẾM số lần sửa và chặn khi vượt hạn mức miễn phí trong báo giá đã chốt
    /// (review 1.1: "quy định số lần sửa và phí sửa"). Đếm ở ĐÂY chứ không ở
    /// <see cref="StartRevisionAsync"/> vì hạn mức là số lần OWNER ĐÒI sửa — provider có bắt tay
    /// vào sửa hay không là chuyện khác.
    /// </summary>
    public async Task<DesignResponse> RequestRevisionAsync(
        Guid accountId, Guid id, RequestDesignRevisionRequest request)
    {
        var design = await LoadForActionAsync(
            accountId, id, "request a design revision", EngagementActor.Owner);

        if (design.Status != DesignStatus.submitted)
            throw new InvalidOperationException(
                $"A revision can only be requested on a design that is 'submitted' (currently: '{design.Status}').");

        var terms = await RevisionPolicy.ResolveAsync(_unitOfWork, design.ProjectWorkingId);

        // Hạn mức đếm trên TOÀN engagement, không trên riêng bản vẽ này — xem
        // RevisionPolicy.CountUsedAsync. designs.revision_count vẫn tăng cho chính nó, nhưng con số
        // đem đi so với hạn mức là tổng của cả hợp tác.
        var usedInEngagement = await RevisionPolicy.CountUsedAsync(_unitOfWork, design.ProjectWorkingId);
        var revisionNo = usedInEngagement + 1;
        var exceedsQuota = terms.Exceeds(revisionNo);

        // Vượt hạn mức mà owner chưa xác nhận chịu phí → 409 kèm con số, KHÔNG âm thầm tính tiền.
        // Không có báo giá chốt free_revision_count thì không gate gì cả (terms.IsUnlimited).
        if (exceedsQuota && !request.AcceptExtraFee)
            throw new InvalidOperationException(
                $"This engagement has used up all {terms.FreeRevisionCount} free design revisions from the agreed quotation. " +
                $"Revision round {revisionNo} will incur a fee " +
                (terms.ExtraRevisionFee is decimal fee
                    ? $"{fee:N0} VND. "
                    : "agreed between the two parties (the provider has not published a rate). ") +
                "Resend with acceptExtraFee = true if you accept.");

        design.Status = DesignStatus.revision;
        design.Reason = request.Reason;

        // += 1 chứ KHÔNG = revisionNo: revisionNo là số thứ tự trên toàn engagement, gán thẳng vào
        // đây thì bộ đếm riêng của bản vẽ phồng lên và CountUsedAsync cộng trùng ở vòng sau.
        design.RevisionCount += 1;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);

        // Khoản phát sinh nằm CÙNG transaction với việc tăng bộ đếm: tách ra thì có đường chạy
        // owner sửa được vòng vượt hạn mức mà không sinh công nợ tương ứng.
        if (exceedsQuota)
        {
            // Báo giá đã công bố đơn giá sửa ⇒ owner bấm acceptExtraFee là đồng ý đúng con số đó,
            // không còn gì để thương lượng: khoản chốt luôn, và owner đúng là bên đã lập nó.
            var feePublished = terms.ExtraRevisionFee.HasValue;
            var now = DateTime.UtcNow;

            var order = new ChangeOrder
            {
                ProjectWorkingId = design.ProjectWorkingId,
                DesignId = design.Id,
                Kind = ChangeOrderKind.extra_revision,
                Title = $"Design revision fee, round {revisionNo}",
                Reason = request.Reason,
                Amount = terms.ExtraRevisionFee ?? 0m,
                RevisionNo = revisionNo,
                Status = feePublished ? ChangeOrderStatus.accepted : ChangeOrderStatus.pending,

                // Chưa công bố giá thì khoản này là HOÁ ĐƠN provider sắp phát: provider điền số
                // rồi owner duyệt. Bên lập PHẢI là provider — để owner thì EnsureIsRequester chặn
                // provider sửa số tiền, RespondAsync chặn owner tự duyệt khoản của bên mình, và
                // khoản kẹt vĩnh viễn ở 0 đồng không ai gỡ được.
                RequestedByParty = feePublished ? EngagementParty.owner : EngagementParty.provider,

                // Nhánh chờ báo giá không có người lập: hệ thống dựng sẵn chỗ cho provider điền,
                // ghi accountId của owner vào đây là sai vết kiểm toán. Vết của việc owner đã đòi
                // sửa nằm ở DesignId + RevisionNo.
                CreatedBy = feePublished ? accountId : null,
                RespondedBy = feePublished ? accountId : null,
                RespondedAt = feePublished ? now : null,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _unitOfWork.GetRepository<ChangeOrder>().InsertAsync(order);

            // Chốt luôn ⇒ là công nợ thật ngay lúc này, nên phải ra đợt thu ngay lúc này. Cùng
            // transaction, để không có đường chạy nào ghi nợ mà quên đường đòi.
            if (feePublished) await ChangeOrderBilling.TryCreateBatchAsync(_unitOfWork, order);
        }

        await _unitOfWork.CommitAsync();

        // Snapshot vòng sửa — chạy NGOÀI transaction đổi status (best-effort, giống submit/approve).
        // BẮT BUỘC phải có: designs.reason chỉ là MỘT ô và sẽ bị vòng sửa kế tiếp ghi đè, nên nếu
        // không đóng băng lý do vào đây thì lý do của các vòng trước mất vĩnh viễn. Snapshot chụp
        // đúng version + đúng bộ ảnh mà owner đã nhìn khi bấm trả về.
        await TrySnapshotAsync(design, DesignVersionSnapshotKind.revision, snapshottedBy: accountId);

        return DesignResponse.From(design);
    }

    /// <summary>Provider bắt đầu sửa theo yêu cầu: revision → in_progress, version +0.1.</summary>
    public async Task<DesignResponse> StartRevisionAsync(Guid accountId, Guid id)
    {
        var design = await LoadForActionAsync(
            accountId, id, "start revising a design", EngagementActor.Provider);

        if (design.Status != DesignStatus.revision)
            throw new InvalidOperationException(
                $"Only a design that is 'revision' can be started (currently: '{design.Status}').");

        design.Status = DesignStatus.in_progress;
        design.Version += 0.1m;
        design.UpdatedAt = DateTime.UtcNow;

        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        return DesignResponse.From(design);
    }

    public async Task<DesignImageResponse> UploadFileAsync(
        Guid accountId, Guid designId, Stream content, string fileName, string? contentType, long sizeBytes,
        string? caption = null, Guid? uploadedBy = null)
    {
        var design = await LoadForActionAsync(
            accountId, designId, "add a file to a design", EngagementActor.Provider);

        if (design.Status == DesignStatus.approved)
            throw new InvalidOperationException("This design is already approved — no more files can be added.");

        Account? uploader = null;
        if (uploadedBy != null)
        {
            uploader = await _unitOfWork.GetRepository<Account>()
                .SingleOrDefaultAsync(predicate: a => a.Id == uploadedBy)
                ?? throw new KeyNotFoundException($"No account found with id {uploadedBy}.");
        }

        // Nhận cả ảnh render lẫn file bản vẽ (pdf/office). Lưu theo "{role}/{accountId}" của người
        // upload — cùng quy ước với api/files (controller luôn truyền account từ token nếu form
        // không có uploadedBy); "designs" chỉ là chốt chặn cho caller không xác định được người upload.
        var folderPath = uploader != null ? $"{uploader.Role}/{uploader.Id}" : "designs";
        var uploaded = await _fileStorage.UploadAsync(content, fileName, contentType, sizeBytes, folderPath);

        var image = new DesignImage
        {
            DesignId = design.Id,
            ImageUrl = uploaded.ObjectName,
            Caption = caption,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<DesignImage>().InsertAsync(image);
        design.UpdatedAt = DateTime.UtcNow;
        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        // ViewUrl do DesignImageResponse.From resolve từ ObjectName — giống hệt uploaded.Url.
        return DesignImageResponse.From(image);
    }

    public async Task RemoveFileAsync(Guid accountId, Guid designId, Guid imageId)
    {
        var design = await LoadForActionAsync(
            accountId, designId, "delete a design file", EngagementActor.Provider);

        if (design.Status == DesignStatus.approved)
            throw new InvalidOperationException("This design is already approved — files can no longer be deleted.");

        var image = design.DesignImages.FirstOrDefault(i => i.Id == imageId)
            ?? throw new KeyNotFoundException($"No file found with id {imageId} in design {designId}.");

        _unitOfWork.GetRepository<DesignImage>().Delete(image);
        design.UpdatedAt = DateTime.UtcNow;
        _repository.Update(design);
        await _unitOfWork.CommitAsync();

        // Ảnh đã được chụp vào snapshot thì KHÔNG xoá object trên bucket. design_version_images chỉ
        // COPY ObjectName chứ không copy file, nên xoá object sẽ làm HỎNG ảnh của mọi vòng sửa cũ —
        // đúng thứ owner cần mở lại khi xem một revision cũ. Để lại object (rác nhỏ) vẫn hơn là mất
        // lịch sử bản vẽ.
        var stillReferencedBySnapshot = await _unitOfWork.GetRepository<DesignVersionImage>()
            .CountAsync(v => v.ImageUrl == image.ImageUrl) > 0;
        if (stillReferencedBySnapshot) return;

        // Dọn object trên bucket sau khi DB đã commit; object không còn cũng bỏ qua.
        try { await _fileStorage.DeleteAsync(image.ImageUrl); }
        catch (KeyNotFoundException) { }
    }

    /// <summary>Nạp design và chốt quyền theo engagement trong một bước.</summary>
    private async Task<Design> LoadForActionAsync(
        Guid accountId, Guid id, string action, params EngagementActor[] allowed)
    {
        var design = await GetDesignAsync(id);

        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, design.ProjectWorkingId);
        EngagementAuthorization.EnsureActor(actor, action, allowed);

        return design;
    }

    private async Task<Design> GetDesignAsync(Guid id)
    {
        return await _repository.SingleOrDefaultAsync(
            predicate: d => d.Id == id,
            include: q => q.Include(d => d.DesignImages))
            ?? throw new KeyNotFoundException($"No design found with id {id}.");
    }

    // ───────── Design versioning (snapshot khi submit / approve / request-revision) ─────────

    public async Task<PaginationResponse<DesignVersionResponse>> GetVersionsAsync(
        Guid accountId, Guid designId, int pageNumber = 1, int pageSize = 20)
    {
        // Xác nhận design tồn tại + người gọi là một bên của engagement — sai id trả 404.
        _ = await LoadForActionAsync(accountId, designId, "view design history",
            EngagementActor.Owner, EngagementActor.Provider);

        // Full history: mỗi submit/approve đều sinh 1 bản — phân trang để không load hết khi version nhiều.
        // Sắp xếp: bản mới nhất trước (theo snapshotted_at). GetQueryable không có orderBy,
        // nên chain OrderByDescending sau khi Include.
        var paged = await _unitOfWork.GetRepository<DesignVersion>()
            .GetQueryable(
                predicate: v => v.DesignId == designId,
                include: q => q.Include(v => v.Images))
            .OrderByDescending(v => v.SnapshottedAt)
            .ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<DesignVersionResponse>(
            paged.Items.Select(DesignVersionResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<DesignVersionResponse> GetVersionByIdAsync(
        Guid accountId, Guid designId, Guid versionId)
    {
        _ = await LoadForActionAsync(accountId, designId, "view design history",
            EngagementActor.Owner, EngagementActor.Provider);

        var version = await _unitOfWork.GetRepository<DesignVersion>()
            .SingleOrDefaultAsync(
                predicate: v => v.Id == versionId && v.DesignId == designId,
                include: q => q.Include(v => v.Images))
            ?? throw new KeyNotFoundException(
                $"No design version found with id {versionId} in design {designId}.");

        return DesignVersionResponse.From(version);
    }

    /// <summary>
    /// Snapshot nguyên trạng Design + toàn bộ DesignImage hiện tại vào DesignVersion + DesignVersionImage.
    /// Best-effort: bọc try/catch để lỗi snapshot KHÔNG rollback status đã đổi (status là quan trọng hơn lịch sử).
    /// Mỗi lần submit/approve đều sinh 1 version MỚI (không upsert) → giữ đầy đủ full history cho truy nguyên.
    ///
    /// Copy ObjectName (image_url) chứ không reference ảnh gốc — khi ảnh gốc bị xoá sau này,
    /// ảnh trong version vẫn còn truy cập được, chỉ cột original_image_id trở thành null.
    /// </summary>
    private async Task TrySnapshotAsync(Design design, DesignVersionSnapshotKind kind, Guid? snapshottedBy)
    {
        try
        {
            await _unitOfWork.ProcessInTransactionAsync(async () =>
            {
                var versionRepo = _unitOfWork.GetRepository<DesignVersion>();
                var imageRepo = _unitOfWork.GetRepository<DesignVersionImage>();

                // Luôn insert version mới — KHÔNG upsert. Mỗi submit/approve đều sinh 1 bản riêng.
                // Full history → FE có thể duyệt lại từng mốc.
                var version = new DesignVersion
                {
                    DesignId = design.Id,
                    SnapshotKind = kind,
                    Version = design.Version,
                    Title = design.Title,
                    Type = design.Type,
                    Status = design.Status,
                    Reason = design.Reason,
                    ChangeSummary = design.ChangeSummary,
                    CreatedBy = design.CreatedBy,
                    SnapshottedBy = snapshottedBy,
                    CreatedAt = design.CreatedAt,
                    SnapshottedAt = DateTime.UtcNow
                };
                await versionRepo.InsertAsync(version);
                await _unitOfWork.CommitAsync();

                foreach (var img in design.DesignImages)
                {
                    await imageRepo.InsertAsync(new DesignVersionImage
                    {
                        DesignVersionId = version.Id,
                        OriginalImageId = img.Id,
                        ImageUrl = img.ImageUrl, // COPY ObjectName — sống độc lập với ảnh gốc.
                        Caption = img.Caption,
                        UploadedBy = img.UploadedBy,
                        UploadedAt = img.CreatedAt
                    });
                }
                await _unitOfWork.CommitAsync();
            });
        }
        catch (Exception ex)
        {
            // Snapshot fail KHÔNG rollback status đã đổi — chỉ log warning.
            // Dùng Console vì project không inject ILogger; thay bằng logger khi có DI logging.
            Console.WriteLine(
                $"[DesignService] Snapshot failed for design {design.Id} kind={kind}: {ex.Message}");
        }
    }
}
