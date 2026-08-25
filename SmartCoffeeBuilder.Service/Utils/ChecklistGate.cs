using System.Linq.Expressions;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Chốt chặn nghiệm thu theo checklist (review 3: "bổ sung nghiệm thu theo checklist, minh chứng
/// cụ thể, cái nào chưa đạt hay cần sửa cái gì").
///
/// Trước khi có lớp này, checklist chỉ là bản ghi: <c>ChecklistItem.IsRequired</c> được mô tả là
/// "mục bắt buộc thì không được nghiệm thu tổng khi còn pending/failed" nhưng KHÔNG chỗ nào đọc.
/// Hệ quả: owner đóng được milestone / duyệt được design / nghiệm thu được cả engagement trong khi
/// mọi mục bắt buộc vẫn đang 'failed'.
///
/// Luật: mục <see cref="ChecklistItem.IsRequired"/> phải ở <see cref="ChecklistStatus.passed"/>.
/// Mục không bắt buộc chỉ để tham khảo, còn 'pending' cũng không chặn.
/// </summary>
public static class ChecklistGate
{
    /// <summary>Checklist của một bản thiết kế — gọi trước khi owner duyệt design.</summary>
    /// <exception cref="InvalidOperationException">Còn mục bắt buộc chưa đạt (HTTP 409).</exception>
    public static Task EnsureDesignPassedAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid designId, string blockedAction) =>
        EnsureAsync(unitOfWork, c => c.DesignId == designId, "the design", blockedAction);

    /// <summary>Checklist của một hạng mục thi công — gọi trước khi đóng milestone.</summary>
    /// <exception cref="InvalidOperationException">Còn mục bắt buộc chưa đạt (HTTP 409).</exception>
    public static Task EnsureConstructionItemPassedAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid constructionItemId, string blockedAction) =>
        EnsureAsync(unitOfWork, c => c.ConstructionItemId == constructionItemId, "the construction item", blockedAction);

    /// <summary>
    /// TOÀN BỘ checklist của một engagement (mọi design + mọi hạng mục thi công của nó) — gọi khi
    /// nghiệm thu tổng. Vẫn cần dù hai hàm trên đã chặn ở từng cấp: mục nghiệm thu có thể được thêm
    /// SAU khi design đã duyệt / milestone đã đóng, và khi đó chỉ chốt chặn ở đây mới thấy.
    /// </summary>
    /// <exception cref="InvalidOperationException">Còn mục bắt buộc chưa đạt (HTTP 409).</exception>
    public static Task EnsureEngagementPassedAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork, Guid projectWorkingId, string blockedAction) =>
        EnsureAsync(
            unitOfWork,
            c => (c.Design != null && c.Design.ProjectWorkingId == projectWorkingId)
                 || (c.ConstructionItem != null && c.ConstructionItem.ProjectWorkingId == projectWorkingId),
            "this engagement",
            blockedAction);

    /// <summary>
    /// Lấy trạng thái của các mục BẮT BUỘC trong phạm vi rồi đếm tại chỗ. Một checklist chỉ vài
    /// chục dòng nên kéo về đếm rẻ hơn là ghép hai câu đếm riêng, và câu lỗi cần tách bạch
    /// "chưa chấm" với "không đạt" — hai việc phải xử lý khác hẳn nhau.
    /// </summary>
    private static async Task EnsureAsync(
        IUnitOfWork<SmartCafeBuilderContext> unitOfWork,
        Expression<Func<ChecklistItem, bool>> scope,
        string subject,
        string blockedAction)
    {
        var statuses = await unitOfWork.GetRepository<ChecklistItem>()
            .GetListAsync(selector: c => c.Status, predicate: scope.AndRequired());

        var failed = statuses.Count(s => s == ChecklistStatus.failed);
        var pending = statuses.Count(s => s == ChecklistStatus.pending);
        if (failed == 0 && pending == 0) return;

        var parts = new List<string>();
        if (failed > 0) parts.Add($"{failed} item(s) FAILED");
        if (pending > 0) parts.Add($"{pending} item(s) not graded");

        throw new InvalidOperationException(
            $"The acceptance checklist for {subject} still has {string.Join(" and ", parts)} " +
            $"(required items only) — {blockedAction}.");
    }

    /// <summary>Ghép thêm điều kiện <c>IsRequired</c> vào predicate phạm vi, giữ nguyên dạng cây
    /// biểu thức để EF vẫn dịch được sang SQL.</summary>
    private static Expression<Func<ChecklistItem, bool>> AndRequired(
        this Expression<Func<ChecklistItem, bool>> scope)
    {
        var p = scope.Parameters[0];
        var required = Expression.Property(p, nameof(ChecklistItem.IsRequired));
        return Expression.Lambda<Func<ChecklistItem, bool>>(
            Expression.AndAlso(scope.Body, required), p);
    }
}
