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
                "Phải gửi ĐÚNG MỘT trong hai: applyId (báo giá kèm hồ sơ ứng tuyển) " +
                "hoặc projectWorkingId (đã được owner mời trực tiếp).");

        if (request.Items.Count == 0)
            throw new ArgumentException("Báo giá phải có ít nhất một hạng mục.");

        var parties = request.ApplyId != null
            ? await LoadApplyAnchorAsync(request.ApplyId.Value)
            : await LoadEngagementAnchorAsync(request.ProjectWorkingId!.Value);

        // Quyền trước mọi check nghiệp vụ — không để người ngoài dò trạng thái hồ sơ của người khác.
        if (parties.ProviderAccountId != accountId)
            throw new UnauthorizedAccessException(
                "Chỉ provider của chính hồ sơ ứng tuyển / hợp tác này mới được lập báo giá.");

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
        EnsureActor(await ResolveActorAsync(accountId, quotation), "sửa báo giá", QuotationActor.Provider);

        if (quotation.Status != QuotationStatus.draft)
            throw new InvalidOperationException(
                $"Báo giá đang ở trạng thái '{quotation.Status}' — chỉ sửa được khi còn 'draft'. " +
                "Bản đã gửi cho chủ quán thì phát hành bản mới thay vì sửa đè lịch sử.");

        if (request.Title != null) quotation.Title = request.Title;
        if (request.Note != null) quotation.Note = request.Note;
        if (request.EstimatedDurationDays.HasValue) quotation.EstimatedDurationDays = request.EstimatedDurationDays;
        if (request.FreeRevisionCount.HasValue) quotation.FreeRevisionCount = request.FreeRevisionCount;

        // Danh sách gửi lên là THAY TOÀN BỘ: xoá sạch rồi dựng lại, để tổng tiền luôn khớp các dòng.
        // Update() chỉ đánh dấu ĐÚNG entity gốc (không duyệt graph — xem GenericRepository), nên
        // dòng con phải tự insert qua repository của chúng với QuotationId gán tay.
        if (request.Items != null)
        {
            if (request.Items.Count == 0)
                throw new ArgumentException("Báo giá phải có ít nhất một hạng mục.");

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
        EnsureActor(await ResolveActorAsync(accountId, quotation), "gửi báo giá cho chủ quán", QuotationActor.Provider);

        EnsureTransition(quotation.Status, QuotationStatus.sent);

        if (quotation.Items.Count == 0)
            throw new InvalidOperationException("Báo giá chưa có hạng mục nào — không gửi được.");

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
        EnsureActor(await ResolveActorAsync(accountId, quotation), "xoá báo giá", QuotationActor.Provider);

        if (quotation.Status != QuotationStatus.draft)
            throw new InvalidOperationException(
                $"Báo giá đang ở trạng thái '{quotation.Status}' — chỉ xoá được bản nháp chưa gửi.");

        var attachments = quotation.Attachments.Select(a => a.FileUrl).ToList();

        _repository.Delete(quotation);
        await _unitOfWork.CommitAsync();

        // Dọn file trên bucket SAU khi DB commit — best-effort, lỗi ở đây không làm hỏng nghiệp vụ.
        foreach (var file in attachments) await _fileStorage.TryDeleteAsync(file);
    }

    // ───────────────────────── Owner phản hồi ─────────────────────────

    public async Task<QuotationResponse> RequestRevisionAsync(Guid accountId, Guid id, RespondQuotationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Phải nêu lý do khi yêu cầu provider gửi bản báo giá khác.");

        var quotation = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, quotation), "yêu cầu sửa báo giá", QuotationActor.Owner);

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
        EnsureActor(await ResolveActorAsync(accountId, quotation), "từ chối báo giá", QuotationActor.Owner);

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
        EnsureActor(await ResolveActorAsync(accountId, quotation), "duyệt báo giá", QuotationActor.Owner);

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
        EnsureActor(await ResolveActorAsync(accountId, quotation), "đính kèm file vào báo giá", QuotationActor.Provider);

        EnsureNotLocked(quotation, "đính kèm thêm file");

        var objectName = await _fileStorage.NormalizeForStorageAsync(request.FileUrl, "fileUrl")
            ?? throw new ArgumentException("fileUrl không được để trống.");

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
        EnsureActor(await ResolveActorAsync(accountId, quotation), "gỡ file đính kèm", QuotationActor.Provider);

        EnsureNotLocked(quotation, "gỡ file đính kèm");

        var attachment = quotation.Attachments.FirstOrDefault(a => a.Id == attachmentId)
            ?? throw new KeyNotFoundException($"Không tìm thấy file đính kèm {attachmentId} trong báo giá này.");

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
                throw new ArgumentException("Mỗi hạng mục phải có tên.");
            if (item.Quantity <= 0)
                throw new ArgumentException($"Số lượng của hạng mục '{item.Name}' phải lớn hơn 0.");
            if (item.UnitPrice < 0)
                throw new ArgumentException($"Đơn giá của hạng mục '{item.Name}' không được âm.");

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
                $"Tổng tỉ lệ các đợt thanh toán phải bằng 100% (đang là {percentageTotal}%).");

        var sortOrder = 0;
        foreach (var term in terms)
        {
            if (string.IsNullOrWhiteSpace(term.Name))
                throw new ArgumentException("Mỗi đợt thanh toán phải có tên (vd: 'Khi ký hợp đồng').");
            if (term.Percentage == null && term.Amount == null)
                throw new ArgumentException($"Đợt '{term.Name}' phải có tỉ lệ % hoặc số tiền.");
            if (term.Percentage is < 0 or > 100)
                throw new ArgumentException($"Tỉ lệ của đợt '{term.Name}' phải nằm trong khoảng 0–100.");

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
                $"Tổng các đợt thanh toán ({scheduled:N0}) vượt quá giá trị báo giá ({totalAmount:N0}).");

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
            "Báo giá này thuộc về một hồ sơ/hợp tác mà tài khoản đang đăng nhập không tham gia.");
    }

    private static void EnsureActor(QuotationActor actual, string action, params QuotationActor[] allowed)
    {
        if (actual == QuotationActor.Admin || allowed.Contains(actual)) return;

        var who = string.Join(" hoặc ", allowed.Select(
            a => a == QuotationActor.Owner ? "chủ quán" : "nhà cung cấp"));
        throw new UnauthorizedAccessException($"Chỉ {who} của hồ sơ/hợp tác này mới được {action}.");
    }

    private async Task<QuotationParties> LoadApplyAnchorAsync(Guid applyId) =>
        (await _unitOfWork.GetRepository<Apply>().GetListAsync(
            selector: a => new QuotationParties(
                a.Post.ProjectShopOwner.Owner.AccountId,
                a.ServiceProviderProfile.AccountId),
            predicate: a => a.Id == applyId))
        .FirstOrDefault()
        ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ứng tuyển với id {applyId}.");

    private async Task<QuotationParties> LoadEngagementAnchorAsync(Guid projectWorkingId) =>
        (await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
            selector: e => new QuotationParties(
                e.ProjectShopOwner.Owner.AccountId,
                e.ServiceProviderProfile.AccountId),
            predicate: e => e.Id == projectWorkingId))
        .FirstOrDefault()
        ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {projectWorkingId}.");

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
                ?? throw new KeyNotFoundException($"Không tìm thấy hồ sơ ứng tuyển với id {applyId}.");

            if (apply.Status != ApplicationStatus.pending)
                throw new InvalidOperationException(
                    $"Hồ sơ ứng tuyển đang ở trạng thái '{apply.Status}' — chỉ gửi báo giá khi hồ sơ còn 'pending'.");
            return;
        }

        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == projectWorkingId)
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {projectWorkingId}.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Hợp tác đang ở trạng thái '{engagement.Status}' — chỉ gửi báo giá khi đã 'accepted' và chưa ký hợp đồng.");
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
                $"Báo giá v{open.Version} đã được chủ quán duyệt — không lập thêm bản mới.",
            QuotationStatus.sent =>
                $"Báo giá v{open.Version} đang chờ chủ quán phản hồi — chờ phản hồi hoặc để chủ quán yêu cầu bản khác.",
            _ =>
                $"Đang có bản nháp v{open.Version} — sửa tiếp bản đó (PUT /api/quotations/{open.Id}) hoặc xoá rồi lập bản mới."
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
            throw new InvalidOperationException($"Báo giá đã được duyệt và khoá — không {action}.");
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
                $"Không thể chuyển báo giá từ '{current}' sang '{target}'.");
    }

    private static QuotationStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;

        if (!Enum.TryParse<QuotationStatus>(status.Trim().Replace("-", "_"), ignoreCase: true, out var parsed))
            throw new ArgumentException(
                $"Status '{status}' không hợp lệ. Cho phép: draft, sent, revision_requested, accepted, rejected, superseded.");

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
        ?? throw new KeyNotFoundException($"Không tìm thấy báo giá với id {id}.");
}
