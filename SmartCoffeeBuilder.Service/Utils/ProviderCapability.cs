using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Ghép <see cref="Capability"/> của provider với <see cref="ServiceKind"/> của bài đăng.
///
/// Một chỗ duy nhất cho luật này vì nó được dùng ở HAI nơi: lọc danh sách bài
/// (<c>PostService.GetAllAsync</c>) và chặn lúc nộp hồ sơ
/// (<c>ApplyService.ApplyAsync</c>). Hai bản sao lệch nhau nghĩa là provider
/// nhìn thấy bài rồi bấm nộp và ăn 409 — đúng cái mà bộ lọc sinh ra để tránh.
/// </summary>
public static class ProviderCapability
{
    /// <summary>
    /// Các loại bài mà provider có năng lực này được nhìn thấy và nộp hồ sơ.
    ///
    /// Bài <c>both</c> là một provider làm trọn gói thiết kế + thi công, nên chỉ
    /// provider <c>both</c> mới nhận được — designer hay constructor đơn lẻ không
    /// giao nổi phần còn lại.
    /// </summary>
    public static ServiceKind[] VisiblePostKinds(Capability capability) => capability switch
    {
        Capability.designer => [ServiceKind.design],
        Capability.constructor => [ServiceKind.construction],
        Capability.both => [ServiceKind.design, ServiceKind.construction, ServiceKind.both],
        _ => []
    };

    /// <summary>
    /// Provider có năng lực này được nộp hồ sơ vào bài loại đó không.
    /// </summary>
    public static bool CanApplyTo(Capability capability, ServiceKind kind) =>
        Array.IndexOf(VisiblePostKinds(capability), kind) >= 0;
}
