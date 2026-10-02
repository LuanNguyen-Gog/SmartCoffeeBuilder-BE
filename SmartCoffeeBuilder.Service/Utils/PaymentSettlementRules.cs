using System.Linq.Expressions;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Tiền phải sòng phẳng trước khi khép sổ (chốt 02/10/2026). Hai thứ chặn:
/// <list type="bullet">
/// <item>Đợt thanh toán chưa <see cref="PaymentBatchStatus.confirmed"/> — owner tải minh chứng, provider xác nhận.</item>
/// <item>Khoản phát sinh còn <see cref="ChangeOrderStatus.pending"/> — bên kia phải duyệt hoặc từ chối;
/// khoản treo là một khoản tiền chưa ai biết có phải trả hay không.</item>
/// </list>
///
/// Áp ở hai cấp:
/// <list type="bullet">
/// <item><b>Nghiệm thu một engagement</b> (mọi loại hình — design, thi công, both):
/// <c>ProjectWorkingService</c> khi owner nghiệm thu.</item>
/// <item><b>Đóng / xoá dự án + noti nhắc đóng</b>: <c>ProjectShopOwnerService.CompleteAsync</c>,
/// <c>DeleteAsync</c>, <c>NotificationService.NotifyProjectReadyToCloseAsync</c> — viết một chỗ vì ba
/// nơi phải cùng một câu trả lời, giống lý do của <see cref="ProjectClosureRules"/>.</item>
/// </list>
///
/// Phạm vi cấp dự án: mọi engagement TRỪ engagement đã huỷ ngang. Huỷ ngang sau khi ký cần hai bên
/// đồng thuận, nên tiền của phần chưa làm do hai bên tự thoả thuận khi đồng ý dừng — để chúng chặn
/// thì dự án bị khoá vĩnh viễn trên những đợt không bao giờ tới hạn.
/// </summary>
public static class PaymentSettlementRules
{
    /// <summary>Một đợt còn treo, đủ để viết câu lỗi cho owner.</summary>
    public sealed record UnsettledBatch(string Name, PaymentBatchStatus Status, ServiceKind Scope);

    /// <summary>Một khoản phát sinh còn chờ quyết định.</summary>
    public sealed record PendingChangeOrder(string Title, decimal Amount, ServiceKind Scope);

    public sealed record Outstanding(
        IReadOnlyList<UnsettledBatch> Batches, IReadOnlyList<PendingChangeOrder> ChangeOrders)
    {
        public bool Any => Batches.Count > 0 || ChangeOrders.Count > 0;
    }

    /// <summary>Những gì còn treo trên cả dự án (bỏ qua engagement đã huỷ ngang).</summary>
    public static Task<Outstanding> FindForProjectAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid projectShopOwnerId) =>
        FindAsync(unitOfWork,
            b => b.Contract.ProjectWorking.ProjectShopOwnerId == projectShopOwnerId
                 && b.Contract.ProjectWorking.Status != ProviderStatus.terminated,
            o => o.ProjectWorking.ProjectShopOwnerId == projectShopOwnerId
                 && o.ProjectWorking.Status != ProviderStatus.terminated);

    /// <summary>Những gì còn treo trên một engagement.</summary>
    public static Task<Outstanding> FindForEngagementAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid projectWorkingId) =>
        FindAsync(unitOfWork,
            b => b.Contract.ProjectWorkingId == projectWorkingId,
            o => o.ProjectWorkingId == projectWorkingId);

    /// <exception cref="InvalidOperationException">Còn tiền treo (HTTP 409).</exception>
    public static async Task EnsureProjectSettledAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid projectShopOwnerId, string blockedAction) =>
        ThrowIfAny(await FindForProjectAsync(unitOfWork, projectShopOwnerId), blockedAction);

    /// <exception cref="InvalidOperationException">Còn tiền treo (HTTP 409).</exception>
    public static async Task EnsureEngagementSettledAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid projectWorkingId, string blockedAction) =>
        ThrowIfAny(await FindForEngagementAsync(unitOfWork, projectWorkingId), blockedAction);

    private static async Task<Outstanding> FindAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        Expression<Func<PaymentBatch, bool>> batchScope,
        Expression<Func<ChangeOrder, bool>> orderScope)
    {
        var batches = await unitOfWork.GetRepository<PaymentBatch>().GetListAsync(
            selector: b => new UnsettledBatch(b.Name, b.Status, b.Contract.ProjectWorking.ContractType),
            predicate: Combine(batchScope,
                b => b.Contract.Status == ContractStatus.confirmed && b.Status != PaymentBatchStatus.confirmed),
            orderBy: q => q.OrderBy(b => b.Contract.ProjectWorking.ContractType).ThenBy(b => b.SortOrder));

        var orders = await unitOfWork.GetRepository<ChangeOrder>().GetListAsync(
            selector: o => new PendingChangeOrder(o.Title, o.Amount, o.ProjectWorking.ContractType),
            predicate: Combine(orderScope, o => o.Status == ChangeOrderStatus.pending),
            orderBy: q => q.OrderBy(o => o.CreatedAt));

        return new Outstanding(batches.ToList(), orders.ToList());
    }

    private static void ThrowIfAny(Outstanding outstanding, string blockedAction)
    {
        if (!outstanding.Any) return;

        var parts = new List<string>();
        if (outstanding.Batches.Count > 0)
            parts.Add($"{outstanding.Batches.Count} payment instalment(s) are not settled yet: " +
                      string.Join(", ", outstanding.Batches.Select(b =>
                          $"'{b.Name}' ({ProjectSlotRules.ScopeLabel(b.Scope)}, {b.Status})")) +
                      " — upload the payment proof for each one and wait for the provider to confirm it");
        if (outstanding.ChangeOrders.Count > 0)
            parts.Add($"{outstanding.ChangeOrders.Count} change order(s) are still waiting for a decision: " +
                      string.Join(", ", outstanding.ChangeOrders.Select(o =>
                          $"'{o.Title}' ({ProjectSlotRules.ScopeLabel(o.Scope)}, {o.Amount:N0} VND)")) +
                      " — accept or reject each one");

        throw new InvalidOperationException($"{string.Join("; ", parts)} before you {blockedAction}.");
    }

    /// <summary>AND hai biểu thức trên cùng tham số (để EF dịch được thành một WHERE).</summary>
    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var parameter = left.Parameters[0];
        var rightBody = new ReplaceParameter(right.Parameters[0], parameter).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody), parameter);
    }

    private sealed class ReplaceParameter(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
