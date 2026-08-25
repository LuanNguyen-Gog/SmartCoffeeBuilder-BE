using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Điều kiện owner được đóng dự án (nghiệm thu toàn dự án). Luật viết MỘT chỗ vì có hai nơi cần
/// cùng câu trả lời: <c>ProjectShopOwnerService.CompleteAsync</c> (chặn thật) và
/// <c>NotificationService.NotifyProjectReadyToCloseAsync</c> (nhắc owner vào bấm). Hai bên từng
/// giữ hai bản guard chép tay và đã lệch nhau — noti mời đóng một dự án mà guard từ chối.
///
/// Luật: dự án chỉ thuê MỘT phía thì phía đó phải hoàn thành; thuê CẢ HAI phía (design +
/// construction) thì cả hai phải hoàn thành. Chưa phía nào hoàn thành thì không có gì để nghiệm
/// thu — owner chỉ còn đường huỷ dự án.
/// </summary>
public static class ProjectClosureRules
{
    /// <summary>Câu lỗi chỉ owner sang đường huỷ dự án khi dự án không còn gì để nghiệm thu.</summary>
    private const string CancelHint = "the only option is to cancel the project (POST /api/project-shop-owners/{id}/cancel).";

    /// <summary>
    /// Lý do dự án CHƯA đóng được, hoặc <c>null</c> khi đã đủ điều kiện.
    /// Trả chuỗi thay vì throw để noti dùng chung được mà không phải bắt exception.
    /// </summary>
    /// <param name="engagements">Mọi engagement của dự án.</param>
    /// <param name="signedEngagementIds">
    /// Id các engagement ĐÃ TỪNG ký hợp đồng (có contract <c>confirmed</c>). Dùng để phân biệt phía
    /// huỷ ngang sau khi ký (bỏ dở giữa chừng → chặn) với phía huỷ khi chưa ký (chưa từng chạy →
    /// không tính). Huỷ ngang được phép ngay từ lúc engagement <c>accepted</c>, tức có thể xảy ra ở
    /// giai đoạn khảo sát trước khi ký.
    /// </param>
    public static string? FindBlocker(
        IEnumerable<ProjectWorking> projectEngagements, IReadOnlySet<Guid> signedEngagementIds)
    {
        // Collection nguồn là ICollection từ navigation property — vật chất hoá một lần rồi duyệt lại.
        var engagements = projectEngagements as IList<ProjectWorking> ?? projectEngagements.ToList();

        // 1. Chưa thuê ai — không có gì để nghiệm thu.
        if (engagements.Count == 0)
            return $"No provider has joined the project yet, so there is nothing to accept — {CancelHint}";

        // 2. Còn phía đang mở: phải nghiệm thu hoặc huỷ ngang từng bên trước đã.
        var open = engagements
            .Where(e => ProjectSlotRules.OccupyingStatuses.Contains(e.Status))
            .ToList();
        if (open.Count > 0)
            return $"{open.Count} engagement(s) are still open (requested/accepted) — the {DescribeScopes(open)} " +
                   "scope is still running. Accept or terminate each provider before closing the project.";

        // 3. Chưa phía nào hoàn thành — dự án chạy dở rồi đứt, không nghiệm thu được.
        var completed = engagements
            .Where(e => e.Status == ProviderStatus.completed)
            .ToList();
        if (completed.Count == 0)
            return $"No side of the project has been accepted ('completed') yet — {CancelHint}";

        // 4. Phía đã ký hợp đồng rồi bỏ dở: dự án thuê cả hai phía thì cả hai phải hoàn thành.
        var abandoned = engagements
            .Where(e => e.Status == ProviderStatus.terminated && signedEngagementIds.Contains(e.Id))
            .ToList();
        var abandonedScopes = ProjectSlotRules.BaseScopes
            .Where(scope =>
                abandoned.Any(e => ProjectSlotRules.Overlaps(e.ContractType, scope))
                && !completed.Any(e => ProjectSlotRules.Overlaps(e.ContractType, scope)))
            .Select(ProjectSlotRules.ScopeLabel)
            .ToList();
        if (abandonedScopes.Count > 0)
            return $"The {string.Join(" and ", abandonedScopes)} scope of the project has a signed contract but was " +
                   "terminated midway and nobody has finished it. Hire someone to complete it and accept that side " +
                   "before closing the project, or cancel the project if the work will not continue.";

        return null;
    }

    /// <summary>Liệt kê các phạm vi công việc (không trùng lặp) của một nhóm engagement.</summary>
    private static string DescribeScopes(IEnumerable<ProjectWorking> engagements) =>
        string.Join(" and ", engagements
            .Select(e => ProjectSlotRules.ScopeLabel(e.ContractType))
            .Distinct());
}
