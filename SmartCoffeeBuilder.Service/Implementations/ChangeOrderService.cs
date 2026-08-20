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
            .GetQueryable(c => c.ProjectWorkingId == projectWorkingId
                               && (st == null || c.Status == st))
            .OrderByDescending(c => c.CreatedAt);

        var paged = await query.ToPaginationResponseAsync(pageNumber, pageSize);

        return new PaginationResponse<ChangeOrderResponse>(
            paged.Items.Select(ChangeOrderResponse.From),
            paged.TotalItems, paged.PageNumber, paged.PageSize);
    }

    public async Task<ChangeOrderResponse> GetByIdAsync(Guid accountId, Guid id)
    {
        var order = await LoadAsync(id);
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, order.ProjectWorkingId);
        return ChangeOrderResponse.From(order);
    }

    public async Task<ChangeOrderResponse> CreateAsync(Guid accountId, CreateChangeOrderRequest request)
    {
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, request.ProjectWorkingId);

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Khoản phát sinh phải có tiêu đề.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Khoản phát sinh phải có lý do — đây là thứ bên kia đọc để duyệt.");
        if (request.Amount < 0)
            throw new ArgumentException("Số tiền phát sinh không được âm.");

        await EnsureReferencesBelongToEngagementAsync(
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

        return ChangeOrderResponse.From(order);
    }

    public async Task<ChangeOrderResponse> UpdateAsync(
        Guid accountId, Guid id, UpdateChangeOrderRequest request)
    {
        var order = await LoadAsync(id);
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, order.ProjectWorkingId);

        EnsureIsRequester(order, actor, "sửa khoản phát sinh này");

        if (order.Status != ChangeOrderStatus.pending)
            throw new InvalidOperationException(
                $"Khoản phát sinh đã '{order.Status}' — không sửa được nữa. Lập khoản mới nếu cần đổi.");

        if (request.Title != null)
        {
            if (string.IsNullOrWhiteSpace(request.Title))
                throw new ArgumentException("Khoản phát sinh phải có tiêu đề.");
            order.Title = request.Title.Trim();
        }
        if (request.Reason != null)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new ArgumentException("Khoản phát sinh phải có lý do.");
            order.Reason = request.Reason;
        }
        if (request.Amount.HasValue)
        {
            if (request.Amount.Value < 0)
                throw new ArgumentException("Số tiền phát sinh không được âm.");
            order.Amount = request.Amount.Value;
        }
        if (request.Kind != null) order.Kind = ParseKind(request.Kind);

        if (request.DesignId.HasValue || request.ConstructionItemId.HasValue)
        {
            await EnsureReferencesBelongToEngagementAsync(
                order.ProjectWorkingId, request.DesignId, request.ConstructionItemId);
            if (request.DesignId.HasValue) order.DesignId = request.DesignId;
            if (request.ConstructionItemId.HasValue) order.ConstructionItemId = request.ConstructionItemId;
        }

        order.UpdatedAt = DateTime.UtcNow;
        _repository.Update(order);
        await _unitOfWork.CommitAsync();

        return ChangeOrderResponse.From(order);
    }

    public async Task<ChangeOrderResponse> AcceptAsync(Guid accountId, Guid id) =>
        await RespondAsync(accountId, id, ChangeOrderStatus.accepted, rejectReason: null);

    public async Task<ChangeOrderResponse> RejectAsync(
        Guid accountId, Guid id, RejectChangeOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RejectReason))
            throw new ArgumentException("Từ chối khoản phát sinh phải kèm lý do.");

        return await RespondAsync(accountId, id, ChangeOrderStatus.rejected, request.RejectReason);
    }

    public async Task DeleteAsync(Guid accountId, Guid id)
    {
        var order = await LoadAsync(id);
        var actor = await EngagementAuthorization.ResolveActorAsync(
            _unitOfWork, accountId, order.ProjectWorkingId);

        EnsureIsRequester(order, actor, "rút lại khoản phát sinh này");

        if (order.Status != ChangeOrderStatus.pending)
            throw new InvalidOperationException(
                $"Khoản phát sinh đã '{order.Status}' — không rút lại được, nó là vết của một quyết định.");

        _repository.Delete(order);
        await _unitOfWork.CommitAsync();
    }

    public async Task<ChangeOrderSummaryResponse> GetSummaryAsync(Guid accountId, Guid projectWorkingId)
    {
        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, projectWorkingId);

        var orders = await _repository.GetListAsync(
            selector: c => new { c.Status, c.Amount, c.Kind },
            predicate: c => c.ProjectWorkingId == projectWorkingId);

        var contractValue = (await _unitOfWork.GetRepository<Contract>().GetListAsync(
                selector: c => c.AgreedValue,
                predicate: c => c.ProjectWorkingId == projectWorkingId
                                && c.Status == ContractStatus.confirmed))
            .FirstOrDefault();

        var accepted = orders.Where(o => o.Status == ChangeOrderStatus.accepted).ToList();
        var pending = orders.Where(o => o.Status == ChangeOrderStatus.pending).ToList();
        var acceptedAmount = accepted.Sum(o => o.Amount);

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
                .Where(o => o.Kind == ChangeOrderKind.extra_revision).Sum(o => o.Amount)
        };
    }

    public async Task<RevisionQuotaResponse> GetRevisionQuotaAsync(Guid accountId, Guid designId)
    {
        var design = await _unitOfWork.GetRepository<Design>()
            .SingleOrDefaultAsync(predicate: d => d.Id == designId)
            ?? throw new KeyNotFoundException($"Không tìm thấy design với id {designId}.");

        await EngagementAuthorization.ResolveActorAsync(_unitOfWork, accountId, design.ProjectWorkingId);

        var terms = await RevisionPolicy.ResolveAsync(_unitOfWork, design.ProjectWorkingId);

        return new RevisionQuotaResponse
        {
            DesignId = design.Id,
            QuotationId = terms.QuotationId,
            FreeRevisionCount = terms.FreeRevisionCount,
            UsedRevisionCount = design.RevisionCount,
            RemainingFreeRevisions = terms.FreeRevisionCount is int free
                ? Math.Max(0, free - design.RevisionCount)
                : null,
            NextRevisionCharged = terms.Exceeds(design.RevisionCount + 1),
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
                $"Khoản phát sinh này đã '{order.Status}' — mỗi khoản chỉ phản hồi được một lần.");

        // Bên lập KHÔNG tự duyệt khoản của mình. Admin đi xuyên như mọi chỗ khác trong hệ thống.
        if (actor != EngagementActor.Admin && ToParty(actor) == order.RequestedByParty)
            throw new UnauthorizedAccessException(
                "Khoản phát sinh do chính bên bạn lập — phải bên còn lại của hợp tác duyệt hoặc từ chối.");

        order.Status = decision;
        order.RejectReason = decision == ChangeOrderStatus.rejected ? rejectReason : null;
        order.RespondedBy = accountId;
        order.RespondedAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        _repository.Update(order);
        await _unitOfWork.CommitAsync();

        return ChangeOrderResponse.From(order);
    }

    private async Task<Entities.ChangeOrder> LoadAsync(Guid id) =>
        await _repository.SingleOrDefaultAsync(predicate: c => c.Id == id)
        ?? throw new KeyNotFoundException($"Không tìm thấy khoản phát sinh với id {id}.");

    /// <summary>
    /// Design / hạng mục gắn kèm phải thuộc CHÍNH engagement này — nếu không thì khoản phát sinh
    /// trỏ sang dự án của người khác và mọi báo cáo chi phí cộng nhầm chỗ.
    /// </summary>
    /// <exception cref="ArgumentException">Tham chiếu thuộc engagement khác (HTTP 400).</exception>
    private async Task EnsureReferencesBelongToEngagementAsync(
        Guid projectWorkingId, Guid? designId, Guid? constructionItemId)
    {
        if (designId is Guid did)
        {
            var ok = await _unitOfWork.GetRepository<Design>()
                .CountAsync(d => d.Id == did && d.ProjectWorkingId == projectWorkingId) > 0;
            if (!ok) throw new ArgumentException($"Design {did} không thuộc hợp tác này.");
        }

        if (constructionItemId is Guid cid)
        {
            var ok = await _unitOfWork.GetRepository<ConstructionItem>()
                .CountAsync(c => c.Id == cid && c.ProjectWorkingId == projectWorkingId) > 0;
            if (!ok) throw new ArgumentException($"Hạng mục {cid} không thuộc hợp tác này.");
        }
    }

    private static void EnsureIsRequester(Entities.ChangeOrder order, EngagementActor actor, string action)
    {
        if (actor == EngagementActor.Admin) return;
        if (ToParty(actor) == order.RequestedByParty) return;

        throw new UnauthorizedAccessException($"Chỉ bên đã lập khoản phát sinh mới được {action}.");
    }

    private static EngagementParty ToParty(EngagementActor actor) => actor switch
    {
        EngagementActor.Owner => EngagementParty.owner,
        EngagementActor.Provider => EngagementParty.provider,

        // Admin không phải một bên của hợp tác — gọi hàm này với Admin là lỗi lập trình, không
        // phải lỗi người dùng. EnsureIsRequester/RespondAsync đã chặn admin trước khi tới đây.
        _ => throw new InvalidOperationException(
            "Admin không phải một bên của hợp tác — không quy về owner/provider được.")
    };

    private static ChangeOrderKind ParseKind(string raw)
    {
        if (Enum.TryParse<ChangeOrderKind>((raw ?? string.Empty).Trim(), ignoreCase: true, out var parsed))
            return parsed;

        throw new ArgumentException(
            $"Loại phát sinh '{raw}' không hợp lệ. Nhận: {string.Join(", ", Enum.GetNames<ChangeOrderKind>())}.");
    }

    private static ChangeOrderStatus? ParseStatusFilter(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (Enum.TryParse<ChangeOrderStatus>(raw.Trim(), ignoreCase: true, out var parsed)) return parsed;

        throw new ArgumentException(
            $"Trạng thái '{raw}' không hợp lệ. Nhận: {string.Join(", ", Enum.GetNames<ChangeOrderStatus>())}.");
    }
}
