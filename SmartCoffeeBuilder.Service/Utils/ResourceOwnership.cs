using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Ownership check cho tài nguyên thuộc TRỰC TIẾP một account — bài đăng, hồ sơ chủ quán,
/// hồ sơ provider. Trục khác với <see cref="EngagementAuthorization"/>: bên kia dành cho tài
/// nguyên CON của một engagement (design, construction_item, issue…) nên phải resolve qua
/// <c>project_provider</c>; ở đây chỉ có một hop tới account nên không cần bộ máy đó.
///
/// Vì sao cần: <see cref="AccountRole"/> chỉ có {owner, provider, admin}, nên owner A và owner B
/// mang role giống hệt nhau và mọi <c>[Authorize(Roles=)]</c> đều cho cả hai qua. Role gate trả
/// lời "bạn thuộc loại người dùng nào", KHÔNG trả lời "bản ghi này có phải của bạn không" —
/// thiếu vế sau là lỗi IDOR / Broken Object Level Authorization.
/// </summary>
public static class ResourceOwnership
{
    /// <summary>
    /// Người gọi có phải admin không. Admin đi cửa riêng ở mọi check ownership, giống
    /// <see cref="EngagementAuthorization.EnsureActor"/>.
    /// </summary>
    public static async Task<bool> IsAdminAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid accountId)
    {
        var account = await unitOfWork.GetRepository<Account>()
            .SingleOrDefaultAsync(predicate: a => a.Id == accountId && a.DeletedAt == null);
        return account?.Role == AccountRole.admin;
    }

    /// <summary>
    /// Bản ghi phải thuộc về người gọi, trừ khi người gọi là admin.
    /// </summary>
    /// <param name="ownerAccountId">Account id chủ sở hữu bản ghi, resolve từ chính bản ghi đó.</param>
    /// <param name="accountId">Người đang đăng nhập (<c>User.GetAccountId()</c>).</param>
    /// <param name="resource">Danh từ chèn vào câu lỗi, ví dụ "post".</param>
    /// <param name="action">Động từ chèn vào câu lỗi, ví dụ "edit it".</param>
    /// <exception cref="UnauthorizedAccessException">Không phải chủ sở hữu và không phải admin (HTTP 401).</exception>
    public static async Task EnsureOwnerAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        Guid ownerAccountId, Guid accountId, string resource, string action)
    {
        if (ownerAccountId == accountId) return;
        if (await IsAdminAsync(unitOfWork, accountId)) return;

        throw new UnauthorizedAccessException(
            $"This {resource} belongs to another account — only its owner may {action}.");
    }
}
