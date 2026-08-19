using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một mục nghiệm thu của hạng mục design hoặc thi công (review 3: "bổ sung nghiệm thu theo
/// checklist, minh chứng cụ thể, cái nào chưa đạt hay cần sửa cái gì").
///
/// Neo vào ĐÚNG MỘT trong hai — enforce bằng CHECK <c>ck_checklist_items_target</c>:
/// <see cref="DesignId"/> (nghiệm thu bản thiết kế) hoặc <see cref="ConstructionItemId"/>
/// (nghiệm thu hạng mục thi công). Dùng FK cứng thay vì FK mềm kiểu <c>comments</c> để xoá hạng mục
/// là checklist đi theo, không cần service tự cascade.
///
/// Provider dựng danh sách mục cần nghiệm thu; OWNER là người chấm đạt/không đạt kèm minh chứng và
/// ghi chú "cần sửa gì" — đó mới là phần hội đồng yêu cầu.
/// </summary>
public class ChecklistItem
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>designs.id</c>. null khi mục này thuộc hạng mục thi công.</summary>
    public Guid? DesignId { get; set; }

    /// <summary>FK -> <c>construction_items.id</c>. null khi mục này thuộc bản thiết kế.</summary>
    public Guid? ConstructionItemId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Thứ tự hiển thị — id là uuid nên KHÔNG suy ra được thứ tự nhập.</summary>
    public int SortOrder { get; set; }

    /// <summary>Mục bắt buộc thì không được nghiệm thu tổng khi còn 'pending' hoặc 'failed'.</summary>
    public bool IsRequired { get; set; } = true;

    public ChecklistStatus Status { get; set; } = ChecklistStatus.pending;

    /// <summary>ObjectName ảnh/tài liệu minh chứng cho kết quả chấm (bucket GCS).</summary>
    public string? EvidenceUrl { get; set; }

    /// <summary>Ghi chú của owner: chưa đạt ở chỗ nào, cần sửa gì.</summary>
    public string? Note { get; set; }

    /// <summary>Account id người chấm (owner) — lấy từ JWT.</summary>
    public Guid? CheckedBy { get; set; }

    public DateTime? CheckedAt { get; set; }

    /// <summary>Account id người lập mục (thường là provider).</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Design? Design { get; set; }
    public ConstructionItem? ConstructionItem { get; set; }
    public Account? CheckedByAccount { get; set; }
    public Account? CreatedByAccount { get; set; }
}
