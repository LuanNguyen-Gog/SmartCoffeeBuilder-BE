using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.PaymentBatch;
using SmartCoffeeBuilder.Service.DTOs.Responses.PaymentBatch;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Thanh toán owner → provider theo đợt (review 3, 17/08/2026).
///
/// Chốt nghiệp vụ: hệ thống KHÔNG giữ tiền. Owner chuyển khoản thẳng cho provider rồi upload minh
/// chứng; provider xác nhận đã nhận thì đợt mới đóng. Vì vậy trạng thái ở đây là tiến trình ĐỐI
/// CHIẾU giữa hai bên, không phải trạng thái giao dịch ngân hàng — khác hẳn <c>PaymentService</c>
/// (payOS, tiền phí nền tảng thật sự chạy qua hệ thống).
///
/// Không có endpoint tạo đợt: đợt sinh từ điều kiện thanh toán của báo giá lúc hợp đồng được ký
/// (xem <c>ContractService.ConfirmOtpAsync</c>), nên không ai tự thêm đợt ngoài cam kết đã chốt.
/// </summary>
public class PaymentBatchService : IPaymentBatchService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<PaymentBatch> _repository;
    private readonly IFileStorageService _fileStorage;

    public PaymentBatchService(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        IFileStorageService fileStorage)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<PaymentBatch>();
        _fileStorage = fileStorage;
    }

    // ───────────────────────── Đọc ─────────────────────────

    public async Task<PaginationResponse<PaymentBatchResponse>> GetAllAsync(
        Guid accountId, int pageNumber = 1, int pageSize = 10,
        Guid? contractId = null, Guid? projectWorkingId = null, string? status = null)
    {
        var st = ParseStatus(status);
        var isAdmin = await IsAdminAsync(accountId);

        var query = _repository
            .GetQueryable(
                b => (contractId == null || b.ContractId == contractId)
                     && (projectWorkingId == null || b.Contract.ProjectWorkingId == projectWorkingId)
                     && (st == null || b.Status == st)
                     && (isAdmin
                         || b.Contract.ProjectWorking.ProjectShopOwner.Owner.AccountId == accountId
                         || b.Contract.ProjectWorking.ServiceProviderProfile.AccountId == accountId),
                include: BuildInclude())
            .OrderBy(b => b.Contract.CreatedAt).ThenBy(b => b.SortOrder);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<PaymentBatchResponse>(
            paged.Items.Select(PaymentBatchResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<PaymentBatchResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var batch = await LoadWithDetailsAsync(id);
        await ResolveActorAsync(accountId, batch);
        return PaymentBatchResponse.From(batch);
    }

    // ───────────────────────── Owner nộp minh chứng ─────────────────────────

    public async Task<PaymentBatchResponse> SubmitProofAsync(
        Guid accountId, Guid id, SubmitPaymentProofRequest request)
    {
        var batch = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, batch), "submit payment proof", PaymentActor.Owner);

        if (batch.Status == PaymentBatchStatus.confirmed)
            throw new InvalidOperationException(
                "The payment batch has already been confirmed by the provider — no further proof can be submitted.");

        if (request.Amount is <= 0)
            throw new ArgumentException("The amount on the proof must be greater than 0.");

        // Ảnh chứng từ là TUỲ CHỌN: hệ thống không nối ngân hàng và không giữ tiền, nên hành động
        // của chủ quán ở đây chỉ là ĐÁNH DẤU "đợt này tôi đã trả" — một cú bấm là đủ. Có đính ảnh
        // thì tốt hơn cho việc đối chiếu, không có cũng không chặn luồng.
        var objectName = await _fileStorage.NormalizeForStorageAsync(request.ImageUrl, "imageUrl");

        var now = DateTime.UtcNow;

        // Giữ NHIỀU bản ghi thay vì ghi đè: một đợt có thể chuyển làm nhiều lần, và bản bị provider
        // bác vẫn phải còn để đối chiếu về sau.
        await _unitOfWork.GetRepository<PaymentProof>().InsertAsync(new PaymentProof
        {
            PaymentBatchId = batch.Id,
            ImageUrl = objectName,
            Amount = request.Amount,
            TransferredAt = request.TransferredAt ?? now,
            Note = request.Note,
            UploadedBy = accountId,
            CreatedAt = now
        });

        batch.Status = PaymentBatchStatus.proof_submitted;
        batch.ProofSubmittedAt = now;
        batch.RejectReason = null;   // minh chứng mới → lý do bác của vòng trước hết hiệu lực
        batch.UpdatedAt = now;

        _repository.Update(batch);
        await _unitOfWork.CommitAsync();

        return PaymentBatchResponse.From(await LoadWithDetailsAsync(id));
    }

    // ───────────────────────── Provider đối chiếu ─────────────────────────

    public async Task<PaymentBatchResponse> ConfirmAsync(Guid accountId, Guid id)
    {
        var batch = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, batch), "confirm receipt of payment", PaymentActor.Provider);

        if (batch.Status != PaymentBatchStatus.proof_submitted)
            throw new InvalidOperationException(
                $"The payment batch is in status '{batch.Status}' — it can only be confirmed once the shop owner has submitted proof.");

        var now = DateTime.UtcNow;
        batch.Status = PaymentBatchStatus.confirmed;
        batch.ConfirmedAt = now;
        batch.ConfirmedBy = accountId;
        batch.UpdatedAt = now;
        _repository.Update(batch);

        await SyncConstructionItemPaidAsync(batch.ConstructionItemId, isPaid: true, now);

        await _unitOfWork.CommitAsync();

        return PaymentBatchResponse.From(await LoadWithDetailsAsync(id));
    }

    public async Task<PaymentBatchResponse> RejectAsync(Guid accountId, Guid id, RejectPaymentBatchRequest request)
    {
        var batch = await LoadWithDetailsAsync(id);
        EnsureActor(await ResolveActorAsync(accountId, batch), "reject payment proof", PaymentActor.Provider);

        if (batch.Status != PaymentBatchStatus.proof_submitted)
            throw new InvalidOperationException(
                $"The payment batch is in status '{batch.Status}' — only proof awaiting reconciliation can be rejected.");

        var now = DateTime.UtcNow;
        batch.Status = PaymentBatchStatus.rejected;
        batch.RejectReason = request.Reason;
        batch.UpdatedAt = now;

        _repository.Update(batch);
        await _unitOfWork.CommitAsync();

        return PaymentBatchResponse.From(await LoadWithDetailsAsync(id));
    }

    public async Task<PaymentBatchResponse> LinkConstructionItemAsync(
        Guid accountId, Guid id, LinkConstructionItemRequest request)
    {
        var batch = await LoadWithDetailsAsync(id);
        EnsureActor(
            await ResolveActorAsync(accountId, batch),
            "link a construction item to a payment batch", PaymentActor.Provider);

        var now = DateTime.UtcNow;
        var previousItemId = batch.ConstructionItemId;

        if (request.ConstructionItemId != null)
        {
            var item = await _unitOfWork.GetRepository<ConstructionItem>()
                .SingleOrDefaultAsync(predicate: ci => ci.Id == request.ConstructionItemId)
                ?? throw new KeyNotFoundException(
                    $"No construction item found with id {request.ConstructionItemId}.");

            // Hạng mục phải thuộc CHÍNH engagement của hợp đồng — nếu không thì đợt tiền của hợp
            // đồng này lại đánh dấu đã thanh toán cho công việc của một hợp tác khác.
            if (item.ProjectWorkingId != batch.Contract.ProjectWorkingId)
                throw new InvalidOperationException(
                    "The construction item does not belong to the engagement of this contract.");
        }

        batch.ConstructionItemId = request.ConstructionItemId;
        batch.UpdatedAt = now;
        _repository.Update(batch);

        // Đổi liên kết khi đợt ĐÃ xác nhận thì cờ đã-thanh-toán phải chạy theo: gỡ ở hạng mục cũ,
        // bật ở hạng mục mới. Đợt chưa xác nhận thì không đụng gì tới cờ.
        if (batch.Status == PaymentBatchStatus.confirmed)
        {
            await SyncConstructionItemPaidAsync(previousItemId, isPaid: false, now);
            await SyncConstructionItemPaidAsync(request.ConstructionItemId, isPaid: true, now);
        }

        await _unitOfWork.CommitAsync();

        return PaymentBatchResponse.From(await LoadWithDetailsAsync(id));
    }

    /// <summary>
    /// Bật/tắt cờ <c>construction_items.is_paid</c> theo trạng thái các đợt thanh toán gắn vào nó.
    /// Cờ này là bản sao cho tiện đọc, nguồn sự thật vẫn là payment_batches — nên khi tắt phải soi
    /// lại xem còn đợt nào khác đã xác nhận trên cùng hạng mục không.
    /// </summary>
    private async Task SyncConstructionItemPaidAsync(Guid? constructionItemId, bool isPaid, DateTime now)
    {
        if (constructionItemId == null) return;

        var item = await _unitOfWork.GetRepository<ConstructionItem>()
            .SingleOrDefaultAsync(predicate: ci => ci.Id == constructionItemId);
        if (item is null) return;

        if (!isPaid)
        {
            var stillPaid = await _repository.CountAsync(
                b => b.ConstructionItemId == constructionItemId
                     && b.Status == PaymentBatchStatus.confirmed) > 0;
            if (stillPaid) return;
        }

        if (item.IsPaid == isPaid) return;

        item.IsPaid = isPaid;
        item.UpdatedAt = now;
        _unitOfWork.GetRepository<ConstructionItem>().Update(item);
    }

    // ───────────────────────── Quyền ─────────────────────────

    private enum PaymentActor { Owner, Provider, Admin }

    private sealed record EngagementParties(Guid OwnerAccountId, Guid ProviderAccountId);

    /// <summary>
    /// Vai trò xét theo ENGAGEMENT mang hợp đồng của đợt thanh toán — không theo AccountRole:
    /// mọi provider đều mang role 'provider' nên role gate không phân biệt được ai với ai.
    /// </summary>
    private async Task<PaymentActor> ResolveActorAsync(Guid accountId, PaymentBatch batch)
    {
        var parties = (await _unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
                selector: e => new EngagementParties(
                    e.ProjectShopOwner.Owner.AccountId,
                    e.ServiceProviderProfile.AccountId),
                predicate: e => e.Id == batch.Contract.ProjectWorkingId))
            .FirstOrDefault()
            ?? throw new KeyNotFoundException(
                $"No project provider found with id {batch.Contract.ProjectWorkingId}.");

        if (parties.OwnerAccountId == accountId) return PaymentActor.Owner;
        if (parties.ProviderAccountId == accountId) return PaymentActor.Provider;
        if (await IsAdminAsync(accountId)) return PaymentActor.Admin;

        throw new UnauthorizedAccessException(
            "This payment batch belongs to an engagement that the signed-in account is not part of.");
    }

    /// <summary>
    /// Admin đi xuyên được phần ĐỌC nhưng KHÔNG thay hai bên nộp/xác nhận tiền: đây là hành vi
    /// đối chiếu tài chính giữa owner và provider, uỷ quyền cho quản trị viên là mất đối chứng.
    /// </summary>
    private static void EnsureActor(PaymentActor actual, string action, params PaymentActor[] allowed)
    {
        if (allowed.Contains(actual)) return;

        var who = string.Join(" or ", allowed.Select(
            a => a == PaymentActor.Owner ? "the shop owner" : "the provider"));
        throw new UnauthorizedAccessException($"Only {who} of this engagement may {action}.");
    }

    private async Task<bool> IsAdminAsync(Guid accountId)
    {
        var account = await _unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    // ───────────────────────── Tra cứu ─────────────────────────

    private static PaymentBatchStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;

        if (!Enum.TryParse<PaymentBatchStatus>(status.Trim().Replace("-", "_"), ignoreCase: true, out var parsed))
            throw new ArgumentException(
                $"Status '{status}' is not valid. Allowed: pending, proof_submitted, confirmed, rejected.");

        return parsed;
    }

    private static Func<IQueryable<PaymentBatch>, IIncludableQueryable<PaymentBatch, object>> BuildInclude() =>
        q => q.Include(b => b.Proofs)
              .Include(b => b.ConstructionItem!)
              .Include(b => b.Contract);

    private async Task<PaymentBatch> LoadWithDetailsAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: b => b.Id == id, include: BuildInclude())
        ?? throw new KeyNotFoundException($"No payment batch found with id {id}.");
}
