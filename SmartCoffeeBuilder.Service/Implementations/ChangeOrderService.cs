using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;
using SmartCoffeeBuilder.Service.ApiResponse;
using SmartCoffeeBuilder.Service.DTOs.Requests.ChangeOrder;
using SmartCoffeeBuilder.Service.DTOs.Responses.ChangeOrder;
using SmartCoffeeBuilder.Service.Interfaces;
using SmartCoffeeBuilder.Service.Utils;
using Entities = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.Implementations;

/// <summary>
/// Phát sinh chi phí ngoài báo giá đã chốt (review 1.1: "quy định số lần sửa và phí sửa").
///
/// Nguyên tắc xuyên suốt: một khoản tiền chỉ thành công nợ khi CẢ HAI BÊN đồng ý. Bên lập không tự
/// duyệt được khoản của chính mình — nếu không thì provider tự cộng tiền vào hợp đồng, hoặc owner
/// tự ghi nhận một khoản giảm giá mà provider không biết.
///
/// Khoản 'accepted' bị KHOÁ: sửa hoặc xoá đều bị chặn, muốn đổi thì lập khoản mới. Cùng nguyên tắc
/// với <c>quotations.locked_at</c> — con số hai bên đã chốt không sửa đè.
///
/// Riêng khoản <see cref="ChangeOrderKind.extra_revision"/> phần lớn do
/// <c>DesignService.RequestRevisionAsync</c> tự sinh khi owner đòi sửa vượt hạn mức miễn phí,
/// không phải lập tay qua đây.
/// </summary>
public class ChangeOrderService : IChangeOrderService
{
    private readonly IUnitOfWork<SmartCafeBuilderContext> _unitOfWork;
    private readonly IGenericRepository<Entities.ChangeOrder> _repository;

    public ChangeOrderService(IUnitOfWork<SmartCafeBuilderContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _repository = unitOfWork.GetRepository<Entities.ChangeOrder>();
    }

    public async Task<PaginationResponse<ChangeOrderResponse>> GetAllAsync(
        Guid accountId, Guid projectWorkingId, string? status = null,
        int pageNumber = 1, int pageSize = 20)
    {
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, projectWorkingId);

        var st = ParseStatusFilter(status);

        var query = _repository
            .GetQueryable(
                c => c.ProjectWorkingId == projectWorkingId
                     && (st == null || c.Status == st),
                // Nạp hạng mục để trả kèm TÊN — FE cần nói "phát sinh này thuộc hạng mục nào".
                include: q => q.Include(c => c.ConstructionItem))
            .OrderByDescending(c => c.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        // Đợt thu của cả trang lấy MỘT lượt: màn hình phát sinh phải nói được khoản nào đã ra tiền,
        // và hỏi từng khoản một là N+1 query cho đúng một cột.
        var batches = await LoadBatchesAsync(paged.Items.Select(c => c.Id));

        return new PaginationResponse<ChangeOrderResponse>(
            paged.Items.Select(c => ChangeOrderResponse.From(c, batches.GetValueOrDefault(c.Id))),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ChangeOrderResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var order = await LoadAsync(id);
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, order.ProjectWorkingId);

        var batches = await LoadBatchesAsync([order.Id]);
        return ChangeOrderResponse.From(order, batches.GetValueOrDefault(order.Id));
    }

    public async Task<ChangeOrderResponse> CreateAsync(Guid accountId, CreateChangeOrderRequest request)
    {
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, request.ProjectWorkingId);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("A change order must have a title.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("A change order must have a reason — this is what the other party reads before approving.");
        if (request.Amount < 0)
            throw new ArgumentException("The change order amount cannot be negative.");

        var anchorName = await EnsureReferencesBelongToEngagementAsync(
            request.ProjectWorkingId, request.DesignId, request.ConstructionItemId);

        var now = DateTime.UtcNow;
        var order = new Entities.ChangeOrder
        {
            ProjectWorkingId = request.ProjectWorkingId,
            DesignId = request.DesignId,
            ConstructionItemId = request.ConstructionItemId,
            Kind = ParseKind(request.Kind),
            Title = request.Title.Trim(),
            Reason = request.Reason,
            Amount = request.Amount,
            Status = ChangeOrderStatus.pending,
            RequestedByParty = ToParty(actor),
            CreatedBy = accountId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repository.InsertAsync(order);
        await _unitOfWork.CommitAsync();

        // Điền tên vào response: entity vừa dựng trong bộ nhớ nên navigation rỗng, không set thì
        // màn hình vừa lập xong không hiện neo cho tới lượt refetch.
        var created = ChangeOrderResponse.From(order);
        created.ConstructionItemName = anchorName;
        return created;
    }

    public async Task<ChangeOrderResponse> UpdateAsync(
        Guid accountId, Guid id, UpdateChangeOrderRequest request)
    {
        var order = await LoadAsync(id);
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, order.ProjectWorkingId);

        EnsureIsRequester(order, actor, "edit this change order");

        if (order.Status != ChangeOrderStatus.pending)
            throw new InvalidOperationException(
                $"This change order is already '{order.Status}' — it can no longer be edited. Create a new one if something must change.");

        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("A change order must have a title.");
            order.Title = request.Title.Trim();
        }
        if (request.Reason != null)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException("A change order must have a reason.");
            order.Reason = request.Reason;
        }
        if (request.Amount.HasValue)
        {
            if (request.Amount.Value < 0)
                throw new ArgumentException("The change order amount cannot be negative.");
            order.Amount = request.Amount.Value;
        }
        if (request.Kind != null) order.Kind = ParseKind(request.Kind);

        // null = không đổi neo, giữ tên navigation đã nạp từ LoadAsync.
        string? renamedAnchor = null;

        if (request.DesignId.HasValue || request.ConstructionItemId.HasValue)
        {
            var newAnchorName = await EnsureReferencesBelongToEngagementAsync(
                order.ProjectWorkingId, request.DesignId, request.ConstructionItemId);
            if (request.DesignId.HasValue) order.DesignId = request.DesignId;
            if (request.ConstructionItemId.HasValue)
            {
                order.ConstructionItemId = request.ConstructionItemId;
                renamedAnchor = newAnchorName;
            }
        }

        order.UpdatedAt = DateTime.UtcNow;
        _repository.Update(order);
        await _unitOfWork.CommitAsync();

        var updated = ChangeOrderResponse.From(order);
        // Navigation từ LoadAsync vẫn trỏ hạng mục CŨ — không ghi đè thì response trả tên cũ
        // dù id đã đổi.
        if (renamedAnchor != null) updated.ConstructionItemName = renamedAnchor;
        return updated;
    }

    public async Task<ChangeOrderResponse> AcceptAsync(Guid accountId, Guid id) =>
        await RespondAsync(accountId, id, ChangeOrderStatus.accepted, rejectReason: null);

    public async Task<ChangeOrderResponse> RejectAsync(
        Guid accountId, Guid id, RejectChangeOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RejectReason))
            throw new ArgumentException("Rejecting a change order requires a reason.");

        return await RespondAsync(accountId, id, ChangeOrderStatus.rejected, request.RejectReason);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var order = await LoadAsync(id);
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, order.ProjectWorkingId);

        EnsureIsRequester(order, actor, "withdraw this change order");

        if (order.Status != ChangeOrderStatus.pending)
            throw new InvalidOperationException(
                $"This change order is already '{order.Status}' — it cannot be withdrawn; it is the record of a decision.");

        _repository.Delete(order);
        await _unitOfWork.CommitAsync();
    }

    public async Task<ChangeOrderSummaryResponse> GetSummaryAsync(Guid accountId, Guid projectWorkingId)
    {
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, projectWorkingId);

        var orders = await _repository.GetListAsync(
            selector: c => new { c.Id, c.Status, c.Amount, c.Kind },
            predicate: c => c.ProjectWorkingId == projectWorkingId);

        var contractValue = (await _unitOfWork.GetRepository<Contract>().GetListAsync(
                selector: c => c.AgreedValue,
                predicate: c => c.ProjectWorkingId == projectWorkingId
                                && c.Status == ContractStatus.confirmed))
            .FirstOrDefault();

        var accepted = orders.Where(o => o.Status == ChangeOrderStatus.accepted).ToList();
        var pending = orders.Where(o => o.Status == ChangeOrderStatus.pending).ToList();
        var acceptedAmount = accepted.Sum(o => o.Amount);

        // Bao nhiêu phần công nợ đã duyệt thực sự ra được đợt thu, và bao nhiêu đã thu xong. Không
        // có hai con số này thì "đã duyệt" trông như "sẽ đòi được", mà khoản duyệt lúc chưa ký hợp
        // đồng (hoặc khoản 0 đồng) thì không ra đợt nào cả.
        var acceptedIds = accepted.Select(o => o.Id).ToList();
        var batches = acceptedIds.Count == 0
            ? []
            : await _unitOfWork.GetRepository<PaymentBatch>().GetListAsync(
                selector: b => new { b.Amount, b.Status },
                predicate: b => b.ChangeOrderId != null && acceptedIds.Contains(b.ChangeOrderId.Value));

        var billedAmount = batches.Sum(b => b.Amount);

        return new ChangeOrderSummaryResponse
        {
            ProjectWorkingId = projectWorkingId,
            ContractValue = contractValue,
            AcceptedAmount = acceptedAmount,
            PendingAmount = pending.Sum(o => o.Amount),

            // null chứ không phải 0 khi chưa ký hợp đồng: "chưa có gì để cộng" khác "tổng bằng 0".
            TotalCommitted = contractValue.HasValue ? contractValue.Value + acceptedAmount : null,
            AcceptedCount = accepted.Count,
            PendingCount = pending.Count,
            RejectedCount = orders.Count(o => o.Status == ChangeOrderStatus.rejected),
            AcceptedRevisionFee = accepted
                .Where(o => o.Kind == ChangeOrderKind.extra_revision).Sum(o => o.Amount),
            BilledAmount = billedAmount,
            PaidAmount = batches
                .Where(b => b.Status == PaymentBatchStatus.confirmed).Sum(b => b.Amount),

            // Phần đã duyệt mà chưa có đợt nào đòi. Dương = còn tiền hai bên đã đồng ý nhưng chưa
            // vào được đường thu (thường vì chưa ký hợp đồng).
            UnbilledAmount = acceptedAmount - billedAmount
        };
    }

    public async Task<RevisionQuotaResponse> GetRevisionQuotaAsync(Guid accountId, Guid designId)
    {
        var design = await _unitOfWork.GetRepository<Design>()
            .SingleOrDefaultAsync(predicate: d => d.Id == designId)
            ?? throw new KeyNotFoundException($"No design found with id {designId}.");

        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, design.ProjectWorkingId);

        var terms = await RevisionPolicy.ResolveAsync(_unitOfWork, design.ProjectWorkingId);

        // Hạn mức tiêu theo ENGAGEMENT, không theo từng bản vẽ — phải trả về đúng con số mà
        // DesignService.RequestRevisionAsync sẽ đem đi so, nếu không màn hình hứa "còn 2 vòng miễn
        // phí" rồi API lại 409 đòi tiền.
        var usedInEngagement = await RevisionPolicy.CountUsedAsync(_unitOfWork, design.ProjectWorkingId);

        return new RevisionQuotaResponse
        {
            DesignId = design.Id,
            ProjectWorkingId = design.ProjectWorkingId,
            QuotationId = terms.QuotationId,
            FreeRevisionCount = terms.FreeRevisionCount,
            UsedRevisionCount = design.RevisionCount,
            EngagementUsedRevisionCount = usedInEngagement,
            RemainingFreeRevisions = terms.FreeRevisionCount is int free
                ? Math.Max(0, free - usedInEngagement)
                : null,
            NextRevisionCharged = terms.Exceeds(usedInEngagement + 1),
            ExtraRevisionFee = terms.ExtraRevisionFee
        };
    }

    // ───────────────────────── Helper ─────────────────────────

    private async Task<ChangeOrderResponse> RespondAsync(
        Guid accountId, Guid id, ChangeOrderStatus decision, string? rejectReason)
    {
        var order = await LoadAsync(id);
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, order.ProjectWorkingId);

        if (order.Status != ChangeOrderStatus.pending)
            throw new InvalidOperationException(
                $"This change order is already '{order.Status}' — each one can only be responded to once.");

        // Bên lập KHÔNG tự duyệt khoản của mình. Admin đi xuyên như mọi chỗ khác trong hệ thống.
        if (actor != EngagementActor.Admin && ToParty(actor) == order.RequestedByParty)
            throw new UnauthorizedAccessException(
                "This change order was raised by your own side — the other party in the engagement must approve or reject it.");

        order.Status = decision;
        order.RejectReason = decision == ChangeOrderStatus.rejected ? rejectReason : null;
        order.RespondedBy = accountId;
        order.RespondedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        _repository.Update(order);

        // Đồng ý xong là tiền owner NỢ THẬT ⇒ phải ra đợt thu ngay, cùng transaction với cái gật
        // đầu. Tách ra thì có đường chạy duyệt được khoản mà đợt tiền commit hụt, và khoản đó nằm
        // trong tổng công nợ mãi mà không ai đòi.
        PaymentBatch? batch = null;
        if (decision == ChangeOrderStatus.accepted)
            batch = await ChangeOrderBilling.TryCreateBatchAsync(_unitOfWork, order);

        await _unitOfWork.CommitAsync();

        return ChangeOrderResponse.From(order, batch);
    }

    private async Task<Entities.ChangeOrder> LoadAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(
            predicate: c => c.Id == id,
            include: q => q.Include(c => c.ConstructionItem))
        ?? throw new KeyNotFoundException($"No change order found with id {id}.");

    /// <summary>
    /// Đợt thanh toán của từng khoản phát sinh, tra theo lô. Mỗi khoản sinh nhiều nhất một đợt
    /// (<see cref="ChangeOrderBilling"/> chặn trùng), nên map 1–1 là đủ.
    /// </summary>
    private async Task<Dictionary<Guid, PaymentBatch>> LoadBatchesAsync(IEnumerable<Guid> orderIds)
    {
        var ids = orderIds.ToList();
        if (ids.Count == 0) return [];

        var batches = await _unitOfWork.GetRepository<PaymentBatch>().GetListAsync(
            predicate: b => b.ChangeOrderId != null && ids.Contains(b.ChangeOrderId.Value));

        return batches
            .GroupBy(b => b.ChangeOrderId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderBy(b => b.CreatedAt).First());
    }
    /// <summary>
    /// Design / hạng mục gắn kèm phải thuộc CHÍNH engagement này — nếu không thì khoản phát sinh
    /// trỏ sang dự án của người khác và mọi báo cáo chi phí cộng nhầm chỗ. Trả về TÊN hạng mục
    /// để người gọi điền vào response.
    ///
    /// Trả tên chứ KHÔNG trả entity: repository trả bản ghi không tracked, gán nó vào navigation
    /// của khoản mới khiến EF tưởng đang chèn thêm một hạng mục và ném 23505 duplicate key trên
    /// pk_construction_items.
    /// </summary>
    /// <exception cref="ArgumentException">Tham chiếu thuộc engagement khác (HTTP 400).</exception>
    private async Task<string?> EnsureReferencesBelongToEngagementAsync(
        Guid projectWorkingId, Guid? designId, Guid? constructionItemId)
    {
        if (designId is Guid did)
        {
            var ok = await _unitOfWork.GetRepository<Design>()
                .CountAsync(d => d.Id == did && d.ProjectWorkingId == projectWorkingId) > 0;
            if (!ok) throw new ArgumentException($"Design {did} does not belong to this engagement.");
        }

        if (constructionItemId is not Guid cid) return null;

        // Chiếu thẳng ra tên: cùng một lượt đi DB, vừa kiểm được vừa lấy được thứ cần.
        var name = (await _unitOfWork.GetRepository<ConstructionItem>().GetListAsync(
                selector: c => c.Name,
                predicate: c => c.Id == cid && c.ProjectWorkingId == projectWorkingId))
            .FirstOrDefault();

        return name ?? throw new ArgumentException(
            $"Construction item {cid} does not belong to this engagement.");
    }

    private static void EnsureIsRequester(Entities.ChangeOrder order, EngagementActor actor, string action)
    {
        if (actor == EngagementActor.Admin) return;
        if (ToParty(actor) == order.RequestedByParty) return;

        throw new UnauthorizedAccessException($"Only the party that raised the change order may {action}.");
    }

    private static EngagementParty ToParty(EngagementActor actor) => actor switch
    {
        EngagementActor.Owner => EngagementParty.owner,
        EngagementActor.Provider => EngagementParty.provider,

        // Admin không phải một bên của hợp tác — gọi hàm này với Admin là lỗi lập trình, không
        // phải lỗi người dùng. EnsureIsRequester/RespondAsync đã chặn admin trước khi tới đây.
        _ => throw new InvalidOperationException(
            "An admin is not a party to the engagement — they cannot be resolved to owner or provider.")
    };

    private static ChangeOrderKind ParseKind(string raw)
    {
        if (Enum.TryParse<ChangeOrderKind>((raw ?? string.Empty).Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException(
            $"Change order kind '{raw}' is not valid. Accepted: {string.Join(", ", Enum.GetNames<ChangeOrderKind>())}.");
    }

    private static ChangeOrderStatus? ParseStatusFilter(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (Enum.TryParse<ChangeOrderStatus>(raw.Trim(), ignoreCase: true, out var parsed)) return parsed;

        throw new ArgumentException(
            $"Status '{raw}' is not valid. Accepted: {string.Join(", ", Enum.GetNames<ChangeOrderStatus>())}.");
    }
}
