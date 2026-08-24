using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Review;

/// <summary>
/// Nhà cung cấp trả lời công khai một đánh giá (review 1.1). Gọi lại là GHI ĐÈ phản hồi cũ —
/// mỗi review đúng một phản hồi.
/// </summary>
public class ReplyReviewRequest
{
    [Required]
    public string Reply { get; set; } = null!;
}

/// <summary>Ảnh thành phẩm chủ quán đính kèm đánh giá.</summary>
public class ReviewImageRequest
{
    /// <summary>ObjectName trên bucket — upload qua /api/files trước rồi gửi giá trị trả về.</summary>
    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }
    public int? SortOrder { get; set; }
}
