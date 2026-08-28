using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.Contract;
using SmartCoffeeBuilder.Service.DTOs.Responses.Contract;
using SmartCoffeeBuilder.Service.Interfaces;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Vòng đời hợp đồng (State_Diagrams.md §7): drafted → pending_otp → confirmed;
/// drafted/pending_otp → cancelled. `confirmed` là mốc engagement chạy thật
/// (guard tạo design/construction_item), KHÔNG đổi provider_status.
///
/// LƯU Ý: OTP ký contract lưu ngay trên bảng contract (otp_code/otp_expires_at) —
/// KHÁC hoàn toàn với OTP tài khoản (bảng otps / OtpService flow reset mật khẩu).
/// </summary>
public class ContractService : IContractService
{
    // OTP ký hợp đồng: 6 chữ số, sống 5 phút.
    private const int OtpLength = 6;
    private const int OtpExpiryMinutes = 5;

    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Contract> _repository;
    private readonly IEmailService _emailService;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notificationService;

    public ContractService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IEmailService emailService,
        IFileStorageService fileStorage,
        INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Contract>();
        _emailService = emailService;
        _fileStorage = fileStorage;
        _notificationService = notificationService;
    }

    public async Task<PaginationResponse<ContractResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10, Guid? projectWorkingId = null)
    {
        // Hợp đồng là tài liệu RIÊNG của một engagement: chỉ owner của dự án và provider của chính
        // engagement đó được thấy. Lọc ngay trong query — không trả về rồi mới ẩn, để phân trang
        // (TotalItems) cũng đúng theo góc nhìn người gọi.
        var isAdmin = await IsAdminAsync(accountId);

        var query = _repository
            .GetQueryable(c => (projectWorkingId == null || c.ProjectWorkingId == projectWorkingId)
                               && (isAdmin
                                   || c.ProjectWorking.ProjectShopOwner.Owner.AccountId == accountId
                                   || c.ProjectWorking.ServiceProviderProfile.AccountId == accountId))
            .OrderByDescending(c => c.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ContractResponse>(
            paged.Items.Select(ContractResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ContractResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"No contract found with id {id}.");

        // Chỉ cần là một bên bất kỳ của engagement — đọc thì owner và provider ngang nhau.
        await ResolveActorAsync(accountId, contract.ProjectWorkingId);

        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> CreateAsync(Guid accountId, CreateContractRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectWorkingId)
            ?? throw new KeyNotFoundException(
                $"No project provider found with id {request.ProjectWorkingId}.");

        // Quyền trước mọi check trạng thái — không để người ngoài dò trạng thái engagement của người khác.
        EnsureActor(
            await ResolveActorAsync(accountId, engagement.Id),
            "draft a contract for this engagement", ContractActor.Provider);

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"The engagement is in status '{engagement.Status}' — a contract can only be created while the engagement is 'accepted'.");

        await EnsureNoActiveContractAsync(engagement);

        // Hợp đồng dựng từ báo giá đã duyệt: giá trị lấy thẳng từ tổng báo giá, KHÔNG nhận số
        // provider gửi lên (review 3: "các field sau lấy từ báo giá và không cho phép provider
        // thay đổi"). Không gửi quotationId thì vẫn đi luồng lập tay cũ.
        var quotation = await LoadAcceptedQuotationAsync(request.QuotationId, engagement);

        // Thời gian thực hiện: nhận từ request, và nếu chỉ có ngày bắt đầu thì suy ngày kết thúc
        // từ estimated_duration_days của báo giá đã duyệt — con số đó chính là cam kết owner đã
        // đọc khi bấm duyệt, chép sang hợp đồng cho khớp thay vì bắt provider gõ lại.
        var (executionStart, executionEnd) = ResolveExecutionPeriod(
            request.ExecutionStartAt, request.ExecutionEndAt, quotation?.EstimatedDurationDays);

        var contract = new Contract
        {
            ProjectWorkingId = engagement.Id,
            QuotationId = quotation?.Id,
            Title = request.Title,
            PartyInfo = request.PartyInfo,
            Terms = request.Terms,
            AgreedValue = quotation?.TotalAmount ?? request.AgreedValue,
            // File hợp đồng phải upload qua api/files trước; giá trị gửi lên rút về ObjectName.
            DocumentUrl = await _fileStorage.NormalizeForStorageAsync(request.DocumentUrl, "documentUrl"),
            ExecutionStartAt = executionStart,
            ExecutionEndAt = executionEnd,
            Status = ContractStatus.drafted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(contract);
        await _unitOfWork.CommitAsync();

        return ContractResponse.From(contract);
    }

    /// <summary>
    /// Mỗi provider chỉ được giữ ĐÚNG MỘT hợp đồng còn hiệu lực với một dự án tại một thời điểm.
    /// "Còn hiệu lực" = mọi trạng thái TRỪ <see cref="ContractStatus.cancelled"/>:
    /// <c>drafted</c> (đang soạn), <c>pending_otp</c> (đã phát OTP, chờ owner ký) và
    /// <c>confirmed</c> (đã ký) đều chiếm chỗ. Muốn lập bản mới thì phải huỷ bản đang treo trước —
    /// nếu không, owner sẽ nhận nhiều bản hợp đồng song song và không biết bản nào có hiệu lực.
    ///
    /// Guard soi theo cặp (project, provider) chứ không chỉ engagement hiện tại: cùng một cặp có thể
    /// còn engagement cũ mang hợp đồng chưa đóng, và về mặt pháp lý đó vẫn là hợp đồng giữa hai bên đó.
    /// </summary>
    /// <exception cref="InvalidOperationException">Đã có hợp đồng còn hiệu lực (HTTP 409).</exception>
    private async Task EnsureNoActiveContractAsync(ProjectWorking engagement)
    {
        // Ưu tiên báo bản "tiến xa nhất": confirmed → pending_otp → drafted, rồi tới bản mới nhất.
        var active = await _repository.SingleOrDefaultAsync(
            predicate: c => c.Status != ContractStatus.cancelled
                            && c.ProjectWorking.ProjectShopOwnerId == engagement.ProjectShopOwnerId
                            && c.ProjectWorking.ServiceProviderProfileId == engagement.ServiceProviderProfileId,
            orderBy: q => q.OrderByDescending(c => c.Status == ContractStatus.confirmed)
                           .ThenByDescending(c => c.Status == ContractStatus.pending_otp)
                           .ThenByDescending(c => c.CreatedAt));

        if (active is null) return;

        throw new InvalidOperationException(active.Status switch
        {
            ContractStatus.confirmed =>
                $"Contract #{active.Id} ('{active.Title}') has already been signed for this project — no further contract can be created.",
            ContractStatus.pending_otp =>
                $"Contract #{active.Id} ('{active.Title}') has already issued an OTP and is waiting for the shop owner to sign — " +
                $"wait for it to be signed, or cancel it (POST /api/contracts/{active.Id}/cancel) before drafting a new one.",
            _ =>
                $"There is already a draft contract #{active.Id} ('{active.Title}') for this project — " +
                $"edit that one directly (PUT /api/contracts/{active.Id}), or cancel it " +
                $"(POST /api/contracts/{active.Id}/cancel) before drafting a new one."
        });
    }

    public async Task<ContractResponse> UpdateAsync(Guid accountId, Guid id, UpdateContractRequest request)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"No contract found with id {id}.");

        EnsureActor(
            await ResolveActorAsync(accountId, contract.ProjectWorkingId),
            "edit contract content", ContractActor.Provider);

        if (contract.Status != ContractStatus.drafted)
            throw new InvalidOperationException(
                $"The contract is in status '{contract.Status}' — it can only be edited while still 'drafted'.");

        if (request.Title != null) contract.Title = request.Title;
        if (request.PartyInfo != null) contract.PartyInfo = request.PartyInfo;
        if (request.Terms != null) contract.Terms = request.Terms;

        if (request.AgreedValue != null)
        {
            // Hợp đồng dựng từ báo giá thì giá trị là con số owner đã duyệt — provider sửa được ở
            // đây thì bảng báo giá đã ký mất luôn ý nghĩa. Muốn đổi giá phải phát hành báo giá mới.
            if (contract.QuotationId != null)
                throw new InvalidOperationException(
                    "This contract takes its value from a quotation the shop owner approved — it cannot be edited directly. " +
                    "To change the price, cancel the contract and issue a new quotation.");

            contract.AgreedValue = request.AgreedValue;
        }

        if (request.ExecutionStartAt != null || request.ExecutionEndAt != null)
        {
            // Validate trên GIÁ TRỊ SAU KHI GỘP, không phải chỉ trên field vừa gửi: sửa mỗi ngày
            // kết thúc về trước ngày bắt đầu cũ vẫn là khoảng âm.
            var (start, end) = ResolveExecutionPeriod(
                request.ExecutionStartAt ?? contract.ExecutionStartAt,
                request.ExecutionEndAt ?? contract.ExecutionEndAt,
                estimatedDurationDays: null);

            contract.ExecutionStartAt = start;
            contract.ExecutionEndAt = end;
        }

        // File cũ bị thay thì dọn luôn object trên bucket (sau khi DB commit) để khỏi rác.
        string? replacedDocument = null;
        if (request.DocumentUrl != null)
        {
            var newDocument = await _fileStorage.NormalizeForStorageAsync(request.DocumentUrl, "documentUrl");
            if (newDocument != contract.DocumentUrl) replacedDocument = contract.DocumentUrl;
            contract.DocumentUrl = newDocument;
        }

        contract.UpdatedAt = DateTime.UtcNow;

        _repository.Update(contract);
        await _unitOfWork.CommitAsync();

        await _fileStorage.TryDeleteAsync(replacedDocument);

        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> SendOtpAsync(Guid accountId, Guid id)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"No contract found with id {id}.");

        // Nạp engagement MỘT lần cho cả check quyền lẫn email người nhận — nạp hai lần sẽ có hai
        // instance cùng khoá trong một context (reads đều AsNoTracking).
        var engagement = await LoadEngagementForSigningAsync(contract.ProjectWorkingId);

        // Quyền TRƯỚC khi phát mã: endpoint này gửi email và đổi trạng thái hợp đồng, nên người
        // ngoài gọi được là vừa spam owner vừa đẩy hợp đồng sang 'pending_otp'.
        // CHỈ OWNER: mã gửi về email owner và cũng chính owner nhập lại ở confirm-otp, nên owner tự
        // yêu cầu phát mã cho mình. Provider KHÔNG phát hộ được (không ai bấm gửi mã vào hộp thư
        // người khác), admin cũng không — cùng lý do với confirm-otp: chữ ký hợp đồng không uỷ quyền.
        EnsureOwnerOfEngagement(accountId, engagement, "request a contract signing OTP");

        // 'drafted' → phát lần đầu (chuyển 'pending_otp'); 'pending_otp' → phát lại khi mã cũ đã
        // hết hạn. Đã confirmed/cancelled thì chặn.
        if (contract.Status is not (ContractStatus.drafted or ContractStatus.pending_otp))
            throw new InvalidOperationException(
                $"The contract is in status '{contract.Status}' — an OTP can only be sent while 'drafted' or 'pending_otp'.");

        // Bấm lại khi mã hiện tại CÒN HẠN → không gửi gì thêm, giữ nguyên mã owner đang cầm trong
        // hộp thư. Cấp mã mới sẽ vô hiệu hoá mã cũ, người vừa mở mail trước đó ra nhập là sai ngay.
        // Trả contract nguyên trạng (200) để FE đọc OtpExpiresAt mà đếm ngược tới lúc gửi lại được.
        if (!string.IsNullOrEmpty(contract.OtpCode)
            && contract.OtpExpiresAt.HasValue
            && contract.OtpExpiresAt.Value > DateTime.UtcNow)
            return ContractResponse.From(contract);

        var ownerEmail = engagement.ProjectShopOwner?.Owner?.Account?.Email;
        if (string.IsNullOrEmpty(ownerEmail))
            throw new InvalidOperationException(
                "Could not determine the owner's email to send the contract signing OTP.");

        var code = GenerateOtpCode();
        contract.OtpCode = code;
        contract.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
        contract.Status = ContractStatus.pending_otp;
        contract.UpdatedAt = DateTime.UtcNow;

        // Gửi email trước khi commit — email lỗi thì không đổi trạng thái (chưa CommitAsync).
        // Tái sử dụng template OtpEmail có sẵn (không đụng OtpService/OtpRepository tài khoản).
        await _emailService.SendTemplateAsync(
            ownerEmail,
            subject: "Contract signing OTP - Smart Coffee Builder",
            templateName: "OtpEmail",
            placeholders: new Dictionary<string, string>
            {
                ["OtpCode"] = code,
                ["ExpiryMinutes"] = OtpExpiryMinutes.ToString(),
                ["Year"] = DateTime.UtcNow.Year.ToString()
            });

        _repository.Update(contract);
        await _unitOfWork.CommitAsync();

        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> ConfirmOtpAsync(
        Guid accountId, Guid id, ConfirmContractOtpRequest request)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"No contract found with id {id}.");

        // Nạp engagement đúng MỘT lần rồi dùng lại cho cả check quyền lẫn mốc started_at —
        // nạp hai lần sẽ có hai instance cùng khoá trong một context (reads đều AsNoTracking).
        var engagement = await LoadEngagementForSigningAsync(contract.ProjectWorkingId);

        // Quyền trước, OTP sau — không để người ngoài dò mã trên hợp đồng của người khác.
        EnsureOwnerOfEngagement(accountId, engagement);

        EnsureTransition(contract.Status, ContractStatus.confirmed);

        if (string.IsNullOrEmpty(contract.OtpCode) || contract.OtpCode != request.OtpCode)
            throw new InvalidOperationException("The OTP is incorrect.");

        if (contract.OtpExpiresAt == null || contract.OtpExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("The OTP has expired — please request a new one.");

        contract.Status = ContractStatus.confirmed;
        contract.ConfirmedAt = DateTime.UtcNow;
        contract.ConfirmedBy = accountId;
        // Mã dùng một lần — xoá sau khi xác nhận.
        contract.OtpCode = null;
        contract.OtpExpiresAt = null;
        contract.UpdatedAt = DateTime.UtcNow;

        _repository.Update(contract);
        MarkEngagementStarted(engagement);
        await GeneratePaymentBatchesAsync(contract);

        // Một SaveChanges → ký hợp đồng, mốc bắt đầu của engagement/dự án và các đợt thanh toán
        // sinh từ báo giá là atomic.
        await _unitOfWork.CommitAsync();

        // Sau khi commit: lượt ký diễn ra hoàn toàn ở phía owner (mã OTP về hộp thư owner), nên
        // provider không có cách nào biết hợp đồng đã có hiệu lực và đợt thanh toán đã sinh.
        await _notificationService.NotifyContractSignedAsync(contract.Id);

        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> CancelAsync(Guid accountId, Guid id)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"No contract found with id {id}.");

        // Huỷ bản nháp thì bên nào cũng được — provider rút lại bản soạn, owner từ chối ký.
        EnsureActor(
            await ResolveActorAsync(accountId, contract.ProjectWorkingId),
            "cancel the contract", ContractActor.Owner, ContractActor.Provider);

        EnsureTransition(contract.Status, ContractStatus.cancelled);

        contract.Status = ContractStatus.cancelled;
        contract.OtpCode = null;
        contract.OtpExpiresAt = null;
        contract.UpdatedAt = DateTime.UtcNow;

        _repository.Update(contract);
        await _unitOfWork.CommitAsync();

        return ContractResponse.From(contract);
    }

    // ──────────────────────────────────────────────────────────────
    /// <summary>
    /// Kiểm tra transition hợp lệ theo State_Diagrams.md §7.
    /// drafted → pending_otp | cancelled; pending_otp → confirmed | cancelled.
    /// Sai transition → InvalidOperationException (409).
    /// </summary>
    private static void EnsureTransition(ContractStatus current, ContractStatus target)
    {
        var allowed = current switch
        {
            ContractStatus.drafted => target is ContractStatus.pending_otp or ContractStatus.cancelled,
            ContractStatus.pending_otp => target is ContractStatus.confirmed or ContractStatus.cancelled,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"A contract cannot move from '{current}' to '{target}'.");
    }

    /// <summary>
    /// Hợp đồng được ký = engagement chạy thật. Đặt mốc started_at cho engagement và đẩy dự án
    /// briefed → in_progress (chỉ lần đầu — hợp đồng thứ hai trở đi không đổi gì).
    /// KHÔNG đụng provider_status: "đang thực hiện" vẫn là trạng thái derived theo v5.
    /// Chỉ ghi vào change tracker; caller CommitAsync chung một transaction.
    /// </summary>
    private void MarkEngagementStarted(ProjectWorking engagement)
    {
        if (engagement.StartedAt == null)
        {
            engagement.StartedAt = DateTime.UtcNow;
            engagement.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<ProjectWorking>().Update(engagement);
        }

        var project = engagement.ProjectShopOwner;
        if (project is { Status: ProjectStatus.briefed })
        {
            project.Status = ProjectStatus.in_progress;
            project.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<ProjectShopOwner>().Update(project);
        }
    }

    /// <summary>
    /// Chốt khoảng thời gian thực hiện của hợp đồng (review 3: "Thời gian thực hiện").
    ///
    /// Chỉ có ngày bắt đầu + báo giá đã cam kết số ngày ⇒ suy ra ngày kết thúc, tính CẢ ngày đầu
    /// (bắt đầu 01/09, 90 ngày ⇒ kết thúc 29/11) cho khớp cách đọc "thi công trong 90 ngày".
    ///
    /// Web chỉ quản MỐC THỜI GIAN. Điều khoản chung và bảo hành cố ý KHÔNG có cột riêng: chúng nằm
    /// trong file hợp đồng hai bên tự upload (<c>document_url</c>) — số hoá thành field rồi hiển
    /// thị lại là hệ thống đang phát ngôn về nội dung pháp lý của hợp đồng.
    /// </summary>
    /// <exception cref="ArgumentException">Ngày kết thúc nằm trước ngày bắt đầu (HTTP 400).</exception>
    private static (DateOnly? Start, DateOnly? End) ResolveExecutionPeriod(
        DateOnly? start, DateOnly? end, int? estimatedDurationDays)
    {
        if (start is DateOnly s && end is null && estimatedDurationDays is int days && days > 0)
            end = s.AddDays(days - 1);

        if (start is DateOnly from && end is DateOnly to && to < from)
            throw new ArgumentException(
                $"ExecutionEndAt '{to:yyyy-MM-dd}' falls before ExecutionStartAt '{from:yyyy-MM-dd}' — " +
                "the execution period cannot be negative.");

        return (start, end);
    }

    /// <summary>
    /// Nạp báo giá nguồn của hợp đồng và kiểm tra nó thật sự thuộc engagement này, đã được owner
    /// duyệt. Trả null khi provider không gửi quotationId (luồng lập hợp đồng tay như trước).
    ///
    /// Hai đường neo báo giá đều phải chấp nhận: qua hồ sơ ứng tuyển (engagement sinh ra từ apply
    /// nào thì nhận báo giá của apply đó) và qua lời mời trực tiếp (báo giá treo thẳng vào engagement).
    /// </summary>
    /// <exception cref="KeyNotFoundException">Không có báo giá với id đó (HTTP 404).</exception>
    /// <exception cref="InvalidOperationException">Báo giá của hợp tác khác hoặc chưa được duyệt (HTTP 409).</exception>
    private async Task<Quotation?> LoadAcceptedQuotationAsync(Guid? quotationId, ProjectWorking engagement)
    {
        if (quotationId == null) return null;

        var quotation = await _unitOfWork.GetRepository<Quotation>()
            .SingleOrDefaultAsync(predicate: q => q.Id == quotationId)
            ?? throw new KeyNotFoundException($"No quotation found with id {quotationId}.");

        var belongsToEngagement =
            (quotation.ProjectWorkingId != null && quotation.ProjectWorkingId == engagement.Id)
            || (quotation.ApplyId != null && engagement.ApplyId != null && quotation.ApplyId == engagement.ApplyId);

        if (!belongsToEngagement)
            throw new InvalidOperationException("This quotation does not belong to the engagement the contract is being drafted for.");

        if (quotation.Status != QuotationStatus.accepted)
            throw new InvalidOperationException(
                $"The quotation is in status '{quotation.Status}' — a contract can only be built from a quotation the shop owner approved.");

        return quotation;
    }

    /// <summary>
    /// Ký xong thì cam kết "30% khi ký, 40% khi duyệt concept…" trong báo giá trở thành các đợt
    /// thanh toán thật để hai bên theo dõi (review 3). Chỉ ghi vào change tracker — caller commit
    /// chung transaction với việc ký.
    ///
    /// Không có báo giá nguồn thì không sinh gì: hợp đồng lập tay không có cơ sở nào để chia đợt.
    /// </summary>
    private async Task GeneratePaymentBatchesAsync(Contract contract)
    {
        if (contract.QuotationId == null) return;

        // Ký lại (hoặc chạy lại luồng) không được đẻ thêm đợt trùng.
        var alreadyGenerated = await _unitOfWork.GetRepository<PaymentBatch>()
            .CountAsync(b => b.ContractId == contract.Id) > 0;
        if (alreadyGenerated) return;

        var terms = await _unitOfWork.GetRepository<QuotationPaymentTerm>().GetListAsync(
            predicate: t => t.QuotationId == contract.QuotationId,
            orderBy: q => q.OrderBy(t => t.SortOrder));

        if (terms.Count == 0) return;

        var now = DateTime.UtcNow;
        var batches = terms.Select(term => new PaymentBatch
        {
            ContractId = contract.Id,
            QuotationPaymentTermId = term.Id,
            SortOrder = term.SortOrder,
            Name = term.Name,
            Percentage = term.Percentage,
            Amount = term.Amount,
            Note = term.Condition,
            Status = PaymentBatchStatus.pending,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        await _unitOfWork.GetRepository<PaymentBatch>().InsertRangeAsync(batches);
    }

    /// <summary>Nạp engagement kèm project + owner cho luồng ký hợp đồng (một lần cho cả request).</summary>
    // Include tới Account vì send-otp cần email owner ngay trên instance vừa check quyền
    // (confirm-otp không dùng email, nhưng ký hợp đồng là luồng thưa — thêm một join rẻ hơn
    // là nuôi hai loader gần giống nhau).
    private async Task<ProjectWorking> LoadEngagementForSigningAsync(Guid projectWorkingId) =>
        await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(
                predicate: e => e.Id == projectWorkingId,
                include: q => q.Include(e => e.ProjectShopOwner)
                    .ThenInclude(p => p.Owner).ThenInclude(o => o.Account))
        ?? throw new KeyNotFoundException(
            $"No project provider found with id {projectWorkingId}.");

    /// <summary>Vai trò của người gọi TRONG engagement mang hợp đồng — không phải AccountRole.</summary>
    private enum ContractActor { Owner, Provider, Admin }

    /// <summary>Hai đầu account của một engagement, lấy bằng projection để khỏi nạp cả graph.</summary>
    private sealed record EngagementParties(Guid OwnerAccountId, Guid ProviderAccountId);

    /// <summary>
    /// Người gọi phải là MỘT BÊN của chính engagement mang hợp đồng này (owner của dự án, hoặc
    /// provider của engagement) — admin đi cửa riêng.
    ///
    /// Xét theo ENGAGEMENT chứ không theo dự án và KHÔNG theo AccountRole: hai provider khác nhau
    /// trên cùng một dự án (một design, một construction) có hai engagement riêng nên không đụng
    /// được hợp đồng của nhau, dù cả hai đều mang role 'provider'.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">Không phải bên nào của engagement (HTTP 401).</exception>
    private async Task<ContractActor> ResolveActorAsync(Guid accountId, Guid projectWorkingId)
    {
        var parties = (await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
                selector: e => new EngagementParties(
                    e.ProjectShopOwner.Owner.AccountId,
                    e.ServiceProviderProfile.AccountId),
                predicate: e => e.Id == projectWorkingId))
            .FirstOrDefault()
            ?? throw new KeyNotFoundException(
                $"No project provider found with id {projectWorkingId}.");

        if (parties.OwnerAccountId == accountId) return ContractActor.Owner;
        if (parties.ProviderAccountId == accountId) return ContractActor.Provider;
        if (await IsAdminAsync(accountId)) return ContractActor.Admin;

        throw new UnauthorizedAccessException(
            "This contract belongs to an engagement that the signed-in account is not part of.");
    }

    /// <summary>Admin luôn được phép; còn lại phải nằm trong danh sách vai trò cho phép.</summary>
    private static void EnsureActor(ContractActor actual, string action, params ContractActor[] allowed)
    {
        if (actual == ContractActor.Admin || allowed.Contains(actual)) return;

        var who = string.Join(" or ", allowed.Select(
            a => a == ContractActor.Owner ? "the shop owner" : "the provider"));
        throw new UnauthorizedAccessException($"Only {who} of this engagement may {action}.");
    }

    /// <summary>Admin xem được mọi hợp đồng (phục vụ giám sát/hỗ trợ).</summary>
    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    /// <summary>
    /// Chỉ owner của chính dự án mới thao tác được lên lượt ký của engagement đó — dùng cho CẢ
    /// send-otp lẫn confirm-otp. Cố tình KHÔNG cho admin đi xuyên (khác <see cref="EnsureActor"/>):
    /// chữ ký hợp đồng không uỷ quyền được.
    /// Sai người → UnauthorizedAccessException (401).
    /// </summary>
    private static void EnsureOwnerOfEngagement(
        Guid accountId, ProjectWorking engagement, string action = "confirm the contract")
    {
        if (engagement.ProjectShopOwner?.Owner?.AccountId != accountId)
            throw new UnauthorizedAccessException($"Only the shop owner of this project may {action}.");
    }

    private static string GenerateOtpCode()
    {
        // Mã ngẫu nhiên [000000, 999999] bằng RNG mật mã.
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString().PadLeft(OtpLength, '0');
    }
}
