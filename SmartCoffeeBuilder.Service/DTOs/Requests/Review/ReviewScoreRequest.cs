using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Review;

/// <summary>Điểm cho một tiêu chí đánh giá (ví dụ: quality, schedule, communication, price).</summary>
public class ReviewScoreRequest
{
    [Required]
    [MaxLength(50)]
    public string Dimension { get; set; } = null!;

    [Range(1, 5)]
    public int Score { get; set; }
}
