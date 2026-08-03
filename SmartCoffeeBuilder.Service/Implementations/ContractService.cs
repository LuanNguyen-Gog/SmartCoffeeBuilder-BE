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

    public ContractService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IEmailService emailService,
        IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Contract>();
        _emailService = emailService;
        _fileStorage = fileStorage;
    }

    public async Task<PaginationResponse<ContractResponse>> GetAllAsync(
        int pageNumber = 1, int pageSize = 10, long? projectWorkingId = null)
    {
        var query = _repository
            .GetQueryable(c => projectWorkingId == null || c.ProjectWorkingId == projectWorkingId)
            .OrderByDescending(c => c.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ContractResponse>(
            paged.Items.Select(ContractResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ContractResponse> GetByIdAsync(long id)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy contract với id {id}.");

        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> CreateAsync(CreateContractRequest request)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(predicate: e => e.Id == request.ProjectWorkingId)
            ?? throw new KeyNotFoundException(
                $"Không tìm thấy project provider với id {request.ProjectWorkingId}.");

        if (engagement.Status != ProviderStatus.accepted)
            throw new InvalidOperationException(
                $"Engagement đang ở trạng thái '{engagement.Status}' — chỉ tạo contract khi engagement 'accepted'.");

        // Không tạo hợp đồng mới khi engagement đã có một contract 'confirmed'.
        var hasConfirmed = await _repository
            .CountAsync(c => c.ProjectWorkingId == engagement.Id && c.Status == ContractStatus.confirmed) > 0;
        if (hasConfirmed)
            throw new InvalidOperationException(
                "Engagement đã có contract 'confirmed' — không tạo thêm hợp đồng.");

        var contract = new Contract
        {
            ProjectWorkingId = engagement.Id,
            Title = request.Title,
            PartyInfo = request.PartyInfo,
            Terms = request.Terms,
            AgreedValue = request.AgreedValue,
            // File hợp đồng phải upload qua api/files trước; giá trị gửi lên rút về ObjectName.
            DocumentUrl = await _fileStorage.NormalizeForStorageAsync(request.DocumentUrl, "documentUrl"),
            Status = ContractStatus.drafted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repository.InsertAsync(contract);
        await _unitOfWork.CommitAsync();

        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> UpdateAsync(long id, UpdateContractRequest request)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy contract với id {id}.");

        if (contract.Status != ContractStatus.drafted)
            throw new InvalidOperationException(
                $"Contract đang ở trạng thái '{contract.Status}' — chỉ sửa được khi còn 'drafted'.");

        if (request.Title != null) contract.Title = request.Title;
        if (request.PartyInfo != null) contract.PartyInfo = request.PartyInfo;
        if (request.Terms != null) contract.Terms = request.Terms;
        if (request.AgreedValue != null) contract.AgreedValue = request.AgreedValue;

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

    public async Task<ContractResponse> SendOtpAsync(long id)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy contract với id {id}.");

        // 'drafted' → gửi lần đầu (chuyển 'pending_otp'); 'pending_otp' → GỬI LẠI khi mã cũ
        // hết hạn/thất lạc (giữ nguyên trạng thái, cấp mã mới đè mã cũ). Đã confirmed/cancelled thì chặn.
        if (contract.Status is not (ContractStatus.drafted or ContractStatus.pending_otp))
            throw new InvalidOperationException(
                $"Contract đang ở trạng thái '{contract.Status}' — chỉ gửi OTP khi 'drafted' hoặc 'pending_otp'.");

        var ownerEmail = await ResolveOwnerEmailAsync(contract.ProjectWorkingId);

        var code = GenerateOtpCode();
        contract.OtpCode = code;
        contract.OtpExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes);
        contract.Status = ContractStatus.pending_otp;
        contract.UpdatedAt = DateTime.UtcNow;

        // Gửi email trước khi commit — email lỗi thì không đổi trạng thái (chưa CommitAsync).
        // Tái sử dụng template OtpEmail có sẵn (không đụng OtpService/OtpRepository tài khoản).
        await _emailService.SendTemplateAsync(
            ownerEmail,
            subject: "Mã OTP ký hợp đồng - Smart Coffee Builder",
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
        long accountId, long id, ConfirmContractOtpRequest request)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy contract với id {id}.");

        // Nạp engagement đúng MỘT lần rồi dùng lại cho cả check quyền lẫn mốc started_at —
        // nạp hai lần sẽ có hai instance cùng khoá trong một context (reads đều AsNoTracking).
        var engagement = await LoadEngagementForSigningAsync(contract.ProjectWorkingId);

        // Quyền trước, OTP sau — không để người ngoài dò mã trên hợp đồng của người khác.
        EnsureOwnerOfEngagement(accountId, engagement);

        EnsureTransition(contract.Status, ContractStatus.confirmed);

        if (string.IsNullOrEmpty(contract.OtpCode) || contract.OtpCode != request.OtpCode)
            throw new InvalidOperationException("Mã OTP không đúng.");

        if (contract.OtpExpiresAt == null || contract.OtpExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException("Mã OTP đã hết hạn — vui lòng gửi lại.");

        contract.Status = ContractStatus.confirmed;
        contract.ConfirmedAt = DateTime.UtcNow;
        contract.ConfirmedBy = accountId;
        // Mã dùng một lần — xoá sau khi xác nhận.
        contract.OtpCode = null;
        contract.OtpExpiresAt = null;
        contract.UpdatedAt = DateTime.UtcNow;

        _repository.Update(contract);
        MarkEngagementStarted(engagement);

        // Một SaveChanges → ký hợp đồng và mốc bắt đầu của engagement/dự án là atomic.
        await _unitOfWork.CommitAsync();

        return ContractResponse.From(contract);
    }

    public async Task<ContractResponse> CancelAsync(long id)
    {
        var contract = await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
            ?? throw new KeyNotFoundException($"Không tìm thấy contract với id {id}.");

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
                $"Không thể chuyển contract từ '{current}' sang '{target}'.");
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

    /// <summary>Nạp engagement kèm project + owner cho luồng ký hợp đồng (một lần cho cả request).</summary>
    private async Task<ProjectWorking> LoadEngagementForSigningAsync(long projectWorkingId) =>
        await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(
                predicate: e => e.Id == projectWorkingId,
                include: q => q.Include(e => e.ProjectShopOwner).ThenInclude(p => p.Owner))
        ?? throw new KeyNotFoundException(
            $"Không tìm thấy project provider với id {projectWorkingId}.");

    /// <summary>
    /// Chỉ owner của chính dự án mới ký được hợp đồng của engagement đó.
    /// Sai người → UnauthorizedAccessException (401).
    /// </summary>
    private static void EnsureOwnerOfEngagement(long accountId, ProjectWorking engagement)
    {
        if (engagement.ProjectShopOwner?.Owner?.AccountId != accountId)
            throw new UnauthorizedAccessException(
                "Chỉ chủ quán của dự án này mới xác nhận được hợp đồng.");
    }

    /// <summary>Lấy email owner của engagement để gửi OTP ký hợp đồng.</summary>
    private async Task<string> ResolveOwnerEmailAsync(long projectWorkingId)
    {
        var engagement = await _unitOfWork.GetRepository<ProjectWorking>()
            .SingleOrDefaultAsync(
                predicate: e => e.Id == projectWorkingId,
                include: q => q.Include(e => e.ProjectShopOwner).ThenInclude(p => p.Owner).ThenInclude(o => o.Account))
            ?? throw new KeyNotFoundException(
                $"Không tìm thấy project provider với id {projectWorkingId}.");

        var email = engagement.ProjectShopOwner?.Owner?.Account?.Email;
        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException(
                "Không xác định được email owner để gửi OTP ký hợp đồng.");

        return email;
    }

    private static string GenerateOtpCode()
    {
        // Mã ngẫu nhiên [000000, 999999] bằng RNG mật mã.
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString().PadLeft(OtpLength, '0');
    }
}
