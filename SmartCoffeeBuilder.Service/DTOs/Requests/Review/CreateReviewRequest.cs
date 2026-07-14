using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Review;

/// <summary>
/// Owner đánh giá provider sau khi nghiệm thu — engagement phải ở trạng thái 'completed'.
/// Mỗi engagement chỉ có 1 review.
/// </summary>
public class CreateReviewRequest
{
    [Required]
    public long ProjectProviderId { get; set; }

    [Range(1, 5)]
    public decimal OverallRating { get; set; }

    public string? Comment { get; set; }

    /// <summary>Điểm theo từng tiêu chí (tuỳ chọn) — dimension không được trùng nhau.</summary>
    public List<ReviewScoreRequest> Scores { get; set; } = new();
}
