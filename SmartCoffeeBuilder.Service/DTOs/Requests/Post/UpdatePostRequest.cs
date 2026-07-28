using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Post;

public class UpdatePostRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    /// <summary>design | construction | both</summary>
    public string? ServiceKind { get; set; }

    /// <summary>open | closed | cancelled</summary>
    public string? Status { get; set; }

    /// <summary>
    /// Ngày hết hạn nộp hồ sơ, định dạng yyyy-MM-dd (vd 2026-08-30).
    /// Hạn thực tế được chốt vào 23:59:59 cuối ngày đó theo giờ Việt Nam.
    /// </summary>
    public DateOnly? SubmissionDeadline { get; set; }
}
