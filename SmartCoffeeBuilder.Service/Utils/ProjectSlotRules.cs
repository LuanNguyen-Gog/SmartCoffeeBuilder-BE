using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Mỗi dự án có ĐÚNG HAI chỗ: một chỗ <c>design</c> và một chỗ <c>construction</c>. Một engagement
/// giữ chỗ theo <c>contract_type</c> của nó — <see cref="ServiceKind.both"/> giữ CẢ HAI. Nghĩa là một
/// dự án tối đa có 2 provider đang hoạt động (hoặc chỉ 1 nếu provider đó nhận trọn gói 'both').
///
/// Đây là luật của DỰ ÁN, khác với <c>Capability</c> (năng lực hồ sơ provider). Một công ty capability
/// 'both' vẫn vào được dự án đã có designer — nhưng chỉ ở chỗ construction còn trống, và phải nhận
/// đúng phạm vi 'construction' chứ không phải 'both'.
///
/// Chỗ chỉ bị giữ bởi engagement ĐANG HOẠT ĐỘNG (<see cref="OccupyingStatuses"/>). Khi engagement
/// chuyển completed / rejected / terminated thì chỗ được trả lại và owner thuê người khác được.
/// </summary>
public static class ProjectSlotRules
{
    /// <summary>Trạng thái engagement được coi là đang giữ chỗ của dự án.</summary>
    public static readonly ProviderStatus[] OccupyingStatuses =
    [
        ProviderStatus.requested, ProviderStatus.accepted
    ];

    /// <summary>
    /// Hai phạm vi CƠ SỞ của một dự án. <see cref="ServiceKind.both"/> KHÔNG có mặt ở đây vì nó là
    /// cách gộp cả hai phạm vi này vào một engagement, không phải phạm vi thứ ba.
    /// </summary>
    public static readonly ServiceKind[] BaseScopes =
    [
        ServiceKind.design, ServiceKind.construction
    ];

    /// <summary>
    /// Nhãn của một phạm vi công việc — dùng trong câu lỗi hiển thị cho người dùng,
    /// thay vì đọc thẳng tên enum.
    /// </summary>
    public static string ScopeLabel(ServiceKind kind) => kind switch
    {
        ServiceKind.design => "design",
        ServiceKind.construction => "construction",
        _ => "design & construction"
    };

    /// <summary>
    /// Hai phạm vi công việc có giẫm chân nhau không. 'both' giẫm lên mọi phạm vi; design chỉ giẫm
    /// design, construction chỉ giẫm construction.
    /// </summary>
    public static bool Overlaps(ServiceKind a, ServiceKind b) =>
        a == ServiceKind.both || b == ServiceKind.both || a == b;

    /// <summary>
    /// Chặn khi phạm vi <paramref name="wanted"/> đụng vào chỗ đã có provider đang giữ.
    /// <paramref name="activeKinds"/> là contract_type của MỌI engagement đang hoạt động thuộc dự án.
    /// <paramref name="action"/> mô tả hành động đang bị chặn, để ghép vào câu lỗi.
    /// </summary>
    /// <exception cref="InvalidOperationException">Chỗ tương ứng đã có người giữ (HTTP 409).</exception>
    public static void EnsureSlotFree(
        IEnumerable<ServiceKind> occupiedKinds, ServiceKind wanted, string action)
    {
        var activeKinds = occupiedKinds as IList<ServiceKind> ?? occupiedKinds.ToList();
        if (!activeKinds.Any(k => Overlaps(k, wanted))) return;

        var designTaken = activeKinds.Any(k => Overlaps(k, ServiceKind.design));
        var constructionTaken = activeKinds.Any(k => Overlaps(k, ServiceKind.construction));

        var taken = (designTaken, constructionTaken) switch
        {
            (true, true) => "both the 'design' and 'construction' slots",
            (true, false) => "the 'design' slot",
            _ => "the 'construction' slot"
        };

        var hint = (designTaken, constructionTaken) switch
        {
            (true, true) => "The project has both slots filled and will not take another provider until one side finishes.",
            (true, false) => "Only the 'construction' slot is still free on this project.",
            _ => "Only the 'design' slot is still free on this project."
        };

        throw new InvalidOperationException(
            $"Cannot {action}: the project already has a provider covering {taken}. {hint}");
    }
}
