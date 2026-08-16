using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Thread comment public neo vào một entity bất kỳ qua FK mềm (<see cref="TargetType"/> + <see cref="TargetId"/>).
/// Mục đích: một bảng <c>comments</c> duy nhất phục vụ comment cho nhiều entity (ConstructionItem, Design…).
/// Cascade xoá khi entity cha bị xoá được xử lý tầng service (vì không có FK cứng).
/// </summary>
public class Comment
{
    public Guid Id { get; set; }

    /// <summary>Loại entity mà comment neo vào (construction_item | design).</summary>
    public CommentTargetType TargetType { get; set; }

    /// <summary>Id của entity cha tương ứng với <see cref="TargetType"/>.</summary>
    public Guid TargetId { get; set; }

    /// <summary>Nội dung comment — nullable để hệ thống có thể chỉ đính kèm file/ảnh sau này.</summary>
    public string? Body { get; set; }

    /// <summary>Account id của người viết comment — controller lấy từ JWT.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Account? CreatedByAccount { get; set; }
}