using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Post;

public class CreatePostRequest
{
    [Required]
    public Guid ProjectShopOwnerId { get; set; }

    /// <summary>design | construction | both</summary>
    [Required]
    public string ServiceKind { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Ngày hết hạn nộp hồ sơ, định dạng yyyy-MM-dd (vd 2026-08-30).
    /// Hạn thực tế được chốt vào 23:59:59 cuối ngày đó theo giờ Việt Nam.
    /// </summary>
    public DateOnly? SubmissionDeadline { get; set; }
}
