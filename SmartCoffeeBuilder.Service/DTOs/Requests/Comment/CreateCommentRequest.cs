using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Comment;

/// <summary>
/// Tạo comment neo vào một entity (ConstructionItem hoặc Design). Controller sẽ ghi đè
/// <see cref="CreatedBy"/> bằng account id từ JWT nếu request không truyền lên.
/// </summary>
public class CreateCommentRequest
{
    /// <summary>construction_item | design (chấp nhận cả snake_case lẫn PascalCase).</summary>
    [Required]
    public string TargetType { get; set; } = null!;

    [Required]
    public Guid TargetId { get; set; }

    /// <summary>Nội dung comment. Nullable — mở đường cho việc chỉ đính kèm file/ảnh sau.</summary>
    public string? Body { get; set; }

    /// <summary>Account id người viết — controller mặc định lấy từ User.GetAccountId().</summary>
    public Guid? CreatedBy { get; set; }
}