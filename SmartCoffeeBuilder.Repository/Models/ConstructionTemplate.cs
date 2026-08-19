using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Mẫu quy trình thi công/thiết kế tái dùng được (review 3: "add thêm template cho quá trình thi
/// công"). Provider dựng sẵn một bộ hạng mục + việc con kèm thời lượng ước tính, rồi áp vào từng
/// engagement thay vì gõ lại từ đầu mỗi dự án.
///
/// Áp template KHÔNG tạo liên kết sống: hệ thống COPY sang construction_item/construction_task rồi
/// thôi. Sửa template về sau không đụng tới dự án đã áp — dự án đang chạy mà bị đổi kế hoạch dưới
/// chân là chuyện không ai muốn.
/// </summary>
public class ConstructionTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Phạm vi mẫu này phục vụ (design hay construction) — lọc cho đỡ chọn nhầm.</summary>
    public ServiceKind ServiceKind { get; set; } = ServiceKind.construction;

    /// <summary>true = mọi provider dùng được (mẫu chuẩn của hệ thống); false = mẫu riêng của người tạo.</summary>
    public bool IsPublic { get; set; }

    /// <summary>Account id provider đã tạo mẫu.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Account? CreatedByAccount { get; set; }
    public ICollection<ConstructionTemplateItem> Items { get; set; } = new List<ConstructionTemplateItem>();
}
