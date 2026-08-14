using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>Vai trò của người gọi TRONG một engagement cụ thể — KHÔNG phải <see cref="AccountRole"/>.</summary>
public enum EngagementActor { Owner, Provider, Admin }

/// <summary>
/// Ownership check theo engagement, dùng chung cho mọi tài nguyên con của <c>project_provider</c>
/// (design, construction_item, construction_task…).
///
/// Đây là trục KHÁC với role gate <c>[Authorize(Roles=)]</c>: <see cref="AccountRole"/> chỉ có
/// {owner, provider, admin}, nên hai provider khác nhau trên cùng một dự án mang role giống hệt
/// nhau và mọi role gate đều cho qua cả hai. Chỉ query mới trả lời được "bản ghi này có phải của
/// bạn không" — thiếu nó là lỗi IDOR / Broken Object Level Authorization.
///
/// <c>ProjectWorkingService</c> và <c>ContractService</c> có bản riêng cùng ý tưởng (chúng resolve
/// từ graph đã nạp sẵn); ở đây tách ra để các service còn lại không phải chép thêm lần nữa.
/// </summary>
public static class EngagementAuthorization
{
    /// <summary>Hai đầu account của một engagement, lấy bằng projection để khỏi nạp cả graph.</summary>
    private sealed record EngagementParties(long OwnerAccountId, long ProviderAccountId);

    /// <summary>
    /// Người gọi là owner của dự án hay provider của engagement — admin đi cửa riêng.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Không có engagement với id đó (HTTP 404).</exception>
    /// <exception cref="UnauthorizedAccessException">Không phải bên nào của engagement (HTTP 401).</exception>
    public static async Task<EngagementActor> ResolveActorAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, long accountId, long projectWorkingId)
    {
        var parties = (await unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
                selector: e => new EngagementParties(
                    e.ProjectShopOwner.Owner.AccountId,
                    e.ServiceProviderProfile.AccountId),
                predicate: e => e.Id == projectWorkingId))
            .FirstOrDefault()
            ?? throw new KeyNotFoundException($"Không tìm thấy project provider với id {projectWorkingId}.");

        if (parties.OwnerAccountId == accountId) return EngagementActor.Owner;
        if (parties.ProviderAccountId == accountId) return EngagementActor.Provider;

        var account = await unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        if (account?.Role == AccountRole.admin) return EngagementActor.Admin;

        throw new UnauthorizedAccessException(
            "Tài nguyên này thuộc về một hợp tác mà tài khoản đang đăng nhập không tham gia.");
    }

    /// <summary>Admin luôn được phép; còn lại phải nằm trong danh sách vai trò cho phép.</summary>
    /// <exception cref="UnauthorizedAccessException">Sai vai trò (HTTP 401).</exception>
    public static void EnsureActor(EngagementActor actual, string action, params EngagementActor[] allowed)
    {
        if (actual == EngagementActor.Admin || allowed.Contains(actual)) return;

        var who = string.Join(" hoặc ", allowed.Select(
            a => a == EngagementActor.Owner ? "chủ quán" : "nhà cung cấp"));
        throw new UnauthorizedAccessException($"Chỉ {who} của hợp tác này mới được {action}.");
    }

    /// <summary>
    /// Id các engagement mà tài khoản này là một bên — dùng để LỌC TRONG QUERY ở endpoint danh sách.
    /// Lọc sau khi lấy về sẽ làm sai <c>TotalItems</c> của phân trang. Trả <c>null</c> khi tài khoản
    /// là admin: admin xem tất cả, caller bỏ qua bước lọc.
    /// </summary>
    public static async Task<List<long>?> GetVisibleEngagementIdsAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, long accountId)
    {
        var account = await unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        if (account?.Role == AccountRole.admin) return null;

        var ids = await unitOfWork.GetRepository<ProjectWorking>().GetListAsync(
            selector: e => e.Id,
            predicate: e => e.ProjectShopOwner.Owner.AccountId == accountId
                            || e.ServiceProviderProfile.AccountId == accountId);

        return [.. ids];
    }
}
