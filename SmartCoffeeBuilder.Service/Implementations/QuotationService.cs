using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;
using SmartCoffeeBuilder.Service.DTOs.Responses.Quotation;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Báo giá tiền hợp đồng — bổ sung theo review 3 (17/08/2026).
///
/// Trước đây owner chỉ thấy một dòng chữ <c>Apply.Proposal</c> nên không có cơ sở nào để chọn giữa
/// nhiều provider cùng ứng tuyển. Bảng báo giá thay chỗ đó: hạng mục chi tiết, tổng tiền, thời gian
/// dự kiến, điều kiện thanh toán theo đợt, file đính kèm — owner duyệt bản nào thì bản đó khoá lại
/// và trở thành nguồn dựng hợp đồng.
///
/// Vòng đời: draft → sent → accepted | rejected | revision_requested. KHÔNG sửa đè bản đã gửi —
/// mỗi vòng sửa là một version mới để owner đối chiếu được lịch sử.
/// </summary>
public class QuotationService : IQuotationService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Quotation> _repository;
    private readonly IApplyService _applyService;
    private readonly IFileStorageService _fileStorage;

    public QuotationService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IApplyService applyService,
        IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Quotation>();
        _applyService = applyService;
        _fileStorage = fileStorage;
    }

    // ───────────────────────── Đọc ─────────────────────────

    public async Task<PaginationResponse<QuotationResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? applyId = null, Guid? projectWorkingId = null, Guid? postId = null, string? status = null)
    {
        var st = ParseStatus(status);
        var isAdmin = await IsAdminAsync(accountId);

        // Lọc quyền NGAY TRONG query (không lấy về rồi ẩn): phân trang mới đếm đúng theo góc nhìn
        // người gọi. Hai nhánh neo kiểm riêng vì mỗi báo giá chỉ có đúng một nhánh khác null.
        var query = _repository
            .GetQueryable(
                q => (applyId == null || q.ApplyId == applyId)
                     && (projectWorkingId == null || q.ProjectWorkingId == projectWorkingId)
                     && (postId == null || (q.Apply != null && q.Apply.PostId == postId))
                     && (st == null || q.Status == st)
                     && (isAdmin
                         || (q.Apply != null
                             && (q.Apply.Post.ProjectShopOwner.Owner.AccountId == accountId
                                 || q.Apply.ServiceProviderProfile.AccountId == accountId))
                         || (q.ProjectWorking != null
                             && (q.ProjectWorking.ProjectShopOwner.Owner.AccountId == accountId
                                 || q.ProjectWorking.ServiceProviderProfile.AccountId == accountId))),
                include: BuildInclude())
            .OrderByDescending(q => q.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<QuotationResponse>(
            paged.Items.Select(QuotationResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<QuotationResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var quotation = await LoadWithDetailsAsync(id);
        await ResolveActorAsync(accountId, quotation);
        return QuotationResponse.From(quotation);
    }

    // ───────────────────────── Provider soạn báo giá ─────────────────────────

    public async Task<QuotationResponse> CreateAsync(Guid accountId, CreateQuotationRequest request)
    {
        if ((request.ApplyId == null) == (request.ProjectWorkingId == null))
            throw new ArgumentException(
                "Send EXACTLY ONE of: applyId (quotation attached to an application) " +
                "or projectWorkingId (already invited directly by the owner).");

        if (request.Items.Count == 0)
            throw new ArgumentException("A quotation must have at least one line item.");

        var parties = request.ApplyId != null
            ? await LoadApplyAnchorAsync(request.ApplyId.Value)
            : await LoadEngagementAnchorAsync(request.ProjectWorkingId!.Value);

        // Quyền trước mọi check nghiệp vụ — không để người ngoài dò trạng thái hồ sơ của người khác.
        if (parties.ProviderAccountId != accountId)
            throw new UnauthorizedAccessException(
                "Only the provider behind this application or engagement may create a quotation.");

        await EnsureAnchorOpenAsync(request.ApplyId, request.ProjectWorkingId);
        await EnsureNoOpenQuotationAsync(request.ApplyId, request.ProjectWorkingId);

        var now = DateTime.UtcNow;
        var quotation = new Quotation
        {
            ApplyId = request.ApplyId,
            ProjectWorkingId = request.ProjectWorkingId,
            Version = await NextVersionAsync(request.ApplyId, request.ProjectWorkingId),
            Title = request.Title,
            Note = request.Note,
            EstimatedDurationDays = request.EstimatedDurationDays,
            FreeRevisionCount = request.FreeRevisionCount,
            ExtraRevisionFee = request.ExtraRevisionFee,
            Status = QuotationStatus.draft,
            CreatedBy = accountId,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Gắn qua navigation: InsertAsync duyệt cả graph nên hạng mục + đợt thanh toán được insert
        // cùng lượt và tự nhận QuotationId sau khi khoá chính được sinh.
        quotation.Items = BuildItems(request.Items);
        quotation.TotalAmount = quotation.Items.Sum(i => i.Amount);
        quotation.PaymentTerms = BuildPaymentTerms(request.PaymentTerms, quotation.TotalAmount);

        await _repository.InsertAsync(quotation);
        await _unitOfWork.CommitAsync();

        return QuotationResponse.From(await LoadWithDetailsAsync(quotation.Id));
    }

    public async Task<QuotationResponse> UpdateAsync(Guid accountId, Guid id, UpdateQuotationRequest request)
    {
        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "edit the quotation", QuotationActor.Provider);

        if (quotation.Status != QuotationStatus.draft)
            throw new InvalidOperationException(
                $"The quotation is in status '{quotation.Status}' — it can only be edited while still 'draft'. " +
                "Once sent to the shop owner, issue a new version instead of overwriting history.");

        if (request.Title != null) quotation.Title = request.Title;
        if (request.Note != null) quotation.Note = request.Note;
        if (request.EstimatedDurationDays.HasValue) quotation.EstimatedDurationDays = request.EstimatedDurationDays;
        if (request.FreeRevisionCount.HasValue) quotation.FreeRevisionCount = request.FreeRevisionCount;
        if (request.ExtraRevisionFee.HasValue)
        {
            if (request.ExtraRevisionFee.Value < 0)
                throw new ArgumentException("The extra revision fee cannot be negative.");
            quotation.ExtraRevisionFee = request.ExtraRevisionFee;
        }

        // Danh sách gửi lên là THAY TOÀN BỘ: xoá sạch rồi dựng lại, để tổng tiền luôn khớp các dòng.
        // Update() chỉ đánh dấu ĐÚNG entity gốc (không duyệt graph — xem GenericRepository), nên
        // dòng con phải tự insert qua repository của chúng với QuotationId gán tay.
        if (request.Items != null)
        {
            if (request.Items.Count == 0)
                throw new ArgumentException("A quotation must have at least one line item.");

            _unitOfWork.GetRepository<QuotationItem>().DeleteRange(quotation.Items.ToList());

            var items = BuildItems(request.Items);
            foreach (var item in items) item.QuotationId = quotation.Id;
            await _unitOfWork.GetRepository<QuotationItem>().InsertRangeAsync(items);

            quotation.Items = items;
            quotation.TotalAmount = items.Sum(i => i.Amount);
        }

        // Đợt thanh toán tính theo % của tổng, nên tổng đổi là phải dựng lại — kể cả khi client
        // chỉ sửa hạng mục và không gửi lại phần điều kiện thanh toán.
        if (request.PaymentTerms != null || request.Items != null)
        {
            var termRequests = request.PaymentTerms ?? quotation.PaymentTerms
                .OrderBy(t => t.SortOrder)
                .Select(t => new QuotationPaymentTermRequest
                {
                    Name = t.Name,
                    Percentage = t.Percentage,
                    Amount = t.Percentage == null ? t.Amount : null,
                    Condition = t.Condition
                })
                .ToList();

            _unitOfWork.GetRepository<QuotationPaymentTerm>().DeleteRange(quotation.PaymentTerms.ToList());

            var terms = BuildPaymentTerms(termRequests, quotation.TotalAmount);
            foreach (var term in terms) term.QuotationId = quotation.Id;
            await _unitOfWork.GetRepository<QuotationPaymentTerm>().InsertRangeAsync(terms);

            quotation.PaymentTerms = terms;
        }

        quotation.UpdatedAt = DateTime.UtcNow;
        _repository.Update(quotation);
        await _unitOfWork.CommitAsync();

        return QuotationResponse.From(await LoadWithDetailsAsync(id));
    }

    public async Task<QuotationResponse> SendAsync(Guid accountId, Guid id)
    {
        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "send the quotation to the shop owner", QuotationActor.Provider);

        EnsureTransition(quotation.Status, QuotationStatus.sent);

        if (quotation.Items.Count == 0)
            throw new InvalidOperationException("The quotation has no line items — it cannot be sent.");

        var now = DateTime.UtcNow;
        quotation.Status = QuotationStatus.sent;
        quotation.SentAt = now;
        quotation.UpdatedAt = now;

        _repository.Update(quotation);
        await _unitOfWork.CommitAsync();

        return QuotationResponse.From(quotation);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "delete the quotation", QuotationActor.Provider);

        if (quotation.Status != QuotationStatus.draft)
            throw new InvalidOperationException(
                $"The quotation is in status '{quotation.Status}' — only an unsent draft can be deleted.");

        var attachments = quotation.Attachments.Select(a => a.FileUrl).ToList();

        // Cascade xoá comment gắn vào bản báo giá này — FK mềm (target_type + target_id) nên DB
        // không tự dọn, giống cách ConstructionItemService.DeleteAsync phải làm.
        var commentRepo = _unitOfWork.GetRepository<Comment>();
        var comments = await commentRepo.GetListAsync(
            predicate: c => c.TargetType == CommentTargetType.quotation && c.TargetId == id);
        commentRepo.DeleteRange(comments);

        _repository.Delete(quotation);
        await _unitOfWork.CommitAsync();

        // Dọn file trên bucket SAU khi DB commit — best-effort, lỗi ở đây không làm hỏng nghiệp vụ.
        foreach (var file in attachments) await _fileStorage.TryDeleteAsync(file);
    }

    // ───────────────────────── Owner phản hồi ─────────────────────────

    public async Task<QuotationResponse> RequestRevisionAsync(Guid accountId, Guid id, RespondQuotationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("A reason is required when asking the provider for a different quotation.");

        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "request a quotation revision", QuotationActor.Owner);

        EnsureTransition(quotation.Status, QuotationStatus.revision_requested);

        var now = DateTime.UtcNow;
        quotation.Status = QuotationStatus.revision_requested;
        quotation.RevisionReason = request.Reason;
        quotation.RespondedAt = now;
        quotation.RespondedBy = accountId;
        quotation.UpdatedAt = now;

        _repository.Update(quotation);
        await _unitOfWork.CommitAsync();

        return QuotationResponse.From(quotation);
    }

    public async Task<QuotationResponse> RejectAsync(Guid accountId, Guid id, RespondQuotationRequest request)
    {
        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "reject the quotation", QuotationActor.Owner);

        EnsureTransition(quotation.Status, QuotationStatus.rejected);

        var now = DateTime.UtcNow;
        quotation.Status = QuotationStatus.rejected;
        quotation.RejectReason = request.Reason;
        quotation.RespondedAt = now;
        quotation.RespondedBy = accountId;
        quotation.UpdatedAt = now;

        _repository.Update(quotation);
        await _unitOfWork.CommitAsync();

        return QuotationResponse.From(quotation);
    }

    public async Task<AcceptQuotationResponse> AcceptAsync(Guid accountId, Guid id)
    {
        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "approve the quotation", QuotationActor.Owner);

        EnsureTransition(quotation.Status, QuotationStatus.accepted);

        var now = DateTime.UtcNow;
        quotation.Status = QuotationStatus.accepted;
        quotation.LockedAt = now;
        quotation.RespondedAt = now;
        quotation.RespondedBy = accountId;
        quotation.UpdatedAt = now;
        _repository.Update(quotation);

        // Các bản còn treo của CÙNG chỗ neo mất hiệu lực — owner đã chốt một bản.
        await SupersedeAsync(
            q => q.Id != quotation.Id
                 && ((quotation.ApplyId != null && q.ApplyId == quotation.ApplyId)
                     || (quotation.ProjectWorkingId != null && q.ProjectWorkingId == quotation.ProjectWorkingId)),
            now);

        var result = new AcceptQuotationResponse();

        if (quotation.ApplyId != null)
        {
            // Báo giá của các provider KHÁC trên cùng bài đăng cũng hết hiệu lực, vì hồ sơ của họ
            // sắp bị từ chối theo luật giữ chỗ dự án. Đánh dấu TRƯỚC khi gọi ApplyService để mọi
            // thay đổi nằm chung một SaveChanges.
            var postId = quotation.Apply!.PostId;
            await SupersedeAsync(
                q => q.ApplyId != null && q.Apply!.PostId == postId && q.ApplyId != quotation.ApplyId,
                now);

            // Duyệt báo giá của một hồ sơ ứng tuyển CHÍNH LÀ chọn provider đó: uỷ cho ApplyService
            // để dùng lại nguyên luật giữ chỗ dự án, đóng bài đăng, từ chối hồ sơ còn lại và bắn
            // notification. Hai service dùng chung IUnitOfWork (scoped) nên CommitAsync bên trong
            // lưu luôn phần thay đổi báo giá ở trên — một transaction, không có trạng thái nửa vời.
            result.Engagement = await _applyService.AcceptAsync(quotation.ApplyId.Value);
        }
        else
        {
            await _unitOfWork.CommitAsync();
        }

        result.Quotation = QuotationResponse.From(await LoadWithDetailsAsync(id));
        return result;
    }

    // ───────────────────────── File đính kèm ─────────────────────────

    public async Task<QuotationResponse> AddAttachmentAsync(Guid accountId, Guid id, AddQuotationAttachmentRequest request)
    {
        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "attach a file to the quotation", QuotationActor.Provider);

        EnsureNotLocked(quotation, "attach another file");

        var objectName = await _fileStorage.NormalizeForStorageAsync(request.FileUrl, "fileUrl")
            ?? throw new ArgumentException("fileUrl cannot be empty.");

        await _unitOfWork.GetRepository<QuotationAttachment>().InsertAsync(new QuotationAttachment
        {
            QuotationId = quotation.Id,
            FileUrl = objectName,
            FileName = request.FileName,
            UploadedBy = accountId,
            CreatedAt = DateTime.UtcNow
        });
        await _unitOfWork.CommitAsync();

        return QuotationResponse.From(await LoadWithDetailsAsync(id));
    }

    public async Task RemoveAttachmentAsync(Guid accountId, Guid id, Guid attachmentId)
    {
        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "remove an attachment", QuotationActor.Provider);

        EnsureNotLocked(quotation, "remove an attachment");

        var attachment = quotation.Attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new KeyNotFoundException($"No attachment {attachmentId} was found in this quotation.");

        _unitOfWork.GetRepository<QuotationAttachment>().Delete(attachment);
        await _unitOfWork.CommitAsync();

        await _fileStorage.TryDeleteAsync(attachment.FileUrl);
    }

    // ───────────────────────── Dựng hạng mục & đợt thanh toán ─────────────────────────

    /// <summary>
    /// Thành tiền từng dòng và tổng báo giá đều TÍNH LẠI ở server. Nhận số client gửi lên thì bảng
    /// hạng mục và con số ghi vào hợp đồng có thể lệch nhau mà không ai phát hiện.
    /// </summary>
    private static List<QuotationItem> BuildItems(List<QuotationItemRequest> items)
    {
        var built = new List<QuotationItem>();
        var sortOrder = 0;
        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Name))
                throw new ArgumentException("Every line item must have a name.");
            if (item.Quantity <= 0)
                throw new ArgumentException($"The quantity of line item '{item.Name}' must be greater than 0.");
            if (item.UnitPrice < 0)
                throw new ArgumentException($"The unit price of line item '{item.Name}' cannot be negative.");

            built.Add(new QuotationItem
            {
                Name = item.Name,
                Description = item.Description,
                Unit = item.Unit,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Amount = decimal.Round(item.Quantity * item.UnitPrice, 2),
                Note = item.Note,
                SortOrder = sortOrder++
            });
        }

        return built;
    }

    /// <summary>
    /// Điều kiện thanh toán theo đợt (review 3: "30% khi ký hợp đồng, 40% khi duyệt concept…").
    /// Đợt ghi bằng % thì quy ra tiền theo tổng báo giá; ghi bằng số tuyệt đối thì giữ nguyên.
    /// Chênh lệch làm tròn dồn vào đợt CUỐI để tổng các đợt luôn bằng đúng giá trị báo giá.
    /// </summary>
    private static List<QuotationPaymentTerm> BuildPaymentTerms(
        List<QuotationPaymentTermRequest> terms, decimal totalAmount)
    {
        var built = new List<QuotationPaymentTerm>();
        if (terms.Count == 0) return built;

        var percentageTotal = terms.Sum(t => t.Percentage ?? 0m);
        if (terms.All(t => t.Percentage != null) && decimal.Round(percentageTotal, 2) != 100m)
            throw new ArgumentException(
                $"The payment terms must add up to 100% (currently {percentageTotal}%).");

        var sortOrder = 0;
        foreach (var term in terms)
        {
            if (string.IsNullOrWhiteSpace(term.Name))
                throw new ArgumentException("Every payment term must have a name (e.g. 'On contract signing').");
            if (term.Percentage == null && term.Amount == null)
                throw new ArgumentException($"Term '{term.Name}' must have either a percentage or an amount.");
            if (term.Percentage is < 0 or > 100)
                throw new ArgumentException($"The percentage of term '{term.Name}' must be between 0 and 100.");

            built.Add(new QuotationPaymentTerm
            {
                SortOrder = sortOrder++,
                Name = term.Name,
                Percentage = term.Percentage,
                Amount = term.Percentage != null
                    ? decimal.Round(totalAmount * term.Percentage.Value / 100m, 2)
                    : term.Amount!.Value,
                Condition = term.Condition
            });
        }

        var scheduled = built.Sum(t => t.Amount);
        if (scheduled > totalAmount)
            throw new ArgumentException(
                $"The payment terms total ({scheduled:N0}) exceeds the quotation value ({totalAmount:N0}).");

        // Chỉ dồn phần lẻ khi mọi đợt tính theo % — trường hợp ghi tay số tiền thì phần còn lại là
        // chủ ý của provider (vd: giữ lại phần bảo hành), không được tự ý cộng thêm.
        if (terms.All(t => t.Percentage != null) && scheduled != totalAmount)
        {
            var last = built.OrderBy(t => t.SortOrder).Last();
            last.Amount += totalAmount - scheduled;
        }

        return built;
    }

    // ───────────────────────── Quyền & tra cứu ─────────────────────────

    /// <summary>Vai trò của người gọi TRONG chỗ neo của báo giá — không phải AccountRole.</summary>
    private enum QuotationActor { Owner, Provider, Admin }

    /// <summary>Hai đầu account của chỗ neo, lấy bằng projection để khỏi nạp cả graph.</summary>
    private sealed record QuotationParties(Guid OwnerAccountId, Guid ProviderAccountId);

    private async Task<QuotationActor> ResolveActorAsync(Guid accountId, Quotation quotation)
    {
        var parties = quotation.ApplyId != null
            ? await LoadApplyAnchorAsync(quotation.ApplyId.Value)
            : await LoadEngagementAnchorAsync(quotation.ProjectWorkingId!.Value);

        if (parties.OwnerAccountId == accountId) return QuotationActor.Owner;
        if (parties.ProviderAccountId == accountId) return QuotationActor.Provider;
        if (await IsAdminAsync(accountId)) return QuotationActor.Admin;

        throw new UnauthorizedAccessException(
            "This quotation belongs to an application or engagement that the signed-in account is not part of.");
    }

    private static void EnsureActor(QuotationActor actual, string action, params QuotationActor[] allowed)
    {
        if (actual == QuotationActor.Admin || allowed.Contains(actual)) return;

        var who = string.Join(" or ", allowed.Select(
            a => a == QuotationActor.Owner ? "the shop owner" : "the provider"));
        throw new UnauthorizedAccessException($"Only {who} of this application or engagement may {action}.");
    }

    private async Task<QuotationParties> LoadApplyAnchorAsync(Guid applyId) =>
        (await _unitOfWork.GetRepository<Apply>().GetListAsync(
            selector: a => new QuotationParties(
                a.Post.ProjectShopOwner.Owner.AccountId,
                a.ServiceProviderProfile.AccountId),
            predicate: a => a.Id == applyId))
        .FirstOrDefault()
        ?? throw new KeyNotFoundException($"No application found with id {applyId}.");

    private async Task<QuotationParties> LoadEngagementAnchorAsync(Guid projectWorkingId) =>
        (await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
            selector: e => new QuotationParties(
                e.ProjectShopOwner.Owner.AccountId,
                e.ServiceProviderProfile.AccountId),
            predicate: e => e.Id == projectWorkingId))
        .FirstOrDefault()
        ?? throw new KeyNotFoundException($"No project provider found with id {projectWorkingId}.");

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    // ───────────────────────── Guard nghiệp vụ ─────────────────────────

    /// <summary>
    /// Chỉ báo giá khi chỗ neo còn "sống": hồ sơ ứng tuyển còn chờ xét, hoặc engagement đã được
    /// nhận. Báo giá cho hồ sơ đã bị từ chối / hợp tác đã kết thúc là rác dữ liệu.
    /// </summary>
    private async Task EnsureAnchorOpenAsync(Guid? applyId, Guid? projectWorkingId)
    {
        if (applyId != null)
        {
            var apply = await _unitOfWork.GetRepository<Apply>()
                .SingleOrDefaultAsync(predicate: a => a.Id == applyId)
                ?? throw new KeyNotFoundException($"No application found with id {applyId}.");

            if (apply.Status != ApplicationStatus.pending)
                throw new InvalidOperationException(
                    $"The application is in status '{apply.Status}' — a quotation can only be sent while the application is 'pending'.");
            return;
        }

        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == projectWorkingId)
            ?? throw new KeyNotFoundException($"No project provider found with id {projectWorkingId}.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"The engagement is in status '{engagement.Status}' — a quotation can only be sent once it is 'accepted' and before the contract is signed.");
    }

    /// <summary>
    /// Mỗi chỗ neo chỉ có MỘT bản báo giá đang treo. Không chặn thì provider gửi được nhiều bản
    /// song song và owner không biết bản nào là bản đang có hiệu lực.
    /// </summary>
    private async Task EnsureNoOpenQuotationAsync(Guid? applyId, Guid? projectWorkingId)
    {
        var open = await _repository.SingleOrDefaultAsync(
            predicate: q => ((applyId != null && q.ApplyId == applyId)
                             || (projectWorkingId != null && q.ProjectWorkingId == projectWorkingId))
                            && (q.Status == QuotationStatus.draft
                                || q.Status == QuotationStatus.sent
                                || q.Status == QuotationStatus.accepted),
            orderBy: q => q.OrderByDescending(x => x.Version));

        if (open is null) return;

        throw new InvalidOperationException(open.Status switch
        {
            QuotationStatus.accepted =>
                $"Quotation v{open.Version} has already been approved by the shop owner — no further version can be created.",
            QuotationStatus.sent =>
                $"Quotation v{open.Version} is awaiting the shop owner's response — wait for it, or let the owner request a different version.",
            _ =>
                $"There is already a draft v{open.Version} — keep editing it (PUT /api/quotations/{open.Id}) or delete it before creating a new version."
        });
    }

    private async Task<int> NextVersionAsync(Guid? applyId, Guid? projectWorkingId)
    {
        var versions = await _repository.GetListAsync(
            selector: q => q.Version,
            predicate: q => (applyId != null && q.ApplyId == applyId)
                            || (projectWorkingId != null && q.ProjectWorkingId == projectWorkingId));

        return versions.Count == 0 ? 1 : versions.Max() + 1;
    }

    /// <summary>Đánh dấu 'superseded' cho các bản còn treo khớp điều kiện (chỉ ghi vào tracker).</summary>
    private async Task SupersedeAsync(
        System.Linq.Expressions.Expression<Func<Quotation, bool>> predicate, DateTime now)
    {
        var open = await _repository.GetListAsync(predicate: predicate);
        var affected = open
            .Where(q => q.Status is QuotationStatus.draft
                        or QuotationStatus.sent
                        or QuotationStatus.revision_requested)
            .ToList();

        foreach (var quotation in affected)
        {
            quotation.Status = QuotationStatus.superseded;
            quotation.UpdatedAt = now;
        }

        _repository.UpdateRange(affected);
    }

    private static void EnsureNotLocked(Quotation quotation, string action)
    {
        if (quotation.LockedAt != null)
            throw new InvalidOperationException($"The quotation is approved and locked — cannot {action}.");
    }

    /// <summary>
    /// Transition hợp lệ: draft → sent; sent → accepted | rejected | revision_requested.
    /// 'accepted' là trạng thái cuối (đã khoá), 'revision_requested' chờ provider phát hành bản mới.
    /// </summary>
    private static void EnsureTransition(QuotationStatus current, QuotationStatus target)
    {
        var allowed = current switch
        {
            QuotationStatus.draft => target is QuotationStatus.sent,
            QuotationStatus.sent => target is QuotationStatus.accepted
                or QuotationStatus.rejected
                or QuotationStatus.revision_requested,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"A quotation cannot move from '{current}' to '{target}'.");
    }

    private static QuotationStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;

        if (!Enum.TryParse<QuotationStatus>(status.Trim().Replace("-", "_"), ignoreCase: true, out var parsed))
            throw new ArgumentException(
                $"Status '{status}' is not valid. Allowed: draft, sent, revision_requested, accepted, rejected, superseded.");

        return parsed;
    }

    private static Func<IQueryable<Quotation>, IIncludableQueryable<Quotation, object>> BuildInclude() =>
        q => q.Include(x => x.Items)
              .Include(x => x.PaymentTerms)
              .Include(x => x.Attachments)
              .Include(x => x.Apply!).ThenInclude(a => a.ServiceProviderProfile)
              .Include(x => x.ProjectWorking!).ThenInclude(e => e.ServiceProviderProfile);

    private async Task<Quotation> LoadWithDetailsAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: q => q.Id == id, include: BuildInclude())
        ?? throw new KeyNotFoundException($"No quotation found with id {id}.");
}
