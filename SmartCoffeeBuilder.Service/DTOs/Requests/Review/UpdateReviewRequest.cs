using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Review;

/// <summary>Sửa review — chỉ gửi field cần đổi. Scores nếu gửi sẽ THAY THẾ toàn bộ điểm cũ.</summary>
public class UpdateReviewRequest
{
    [Range(1, 5)]
    public decimal? OverallRating { get; set; }

    public string? Comment { get; set; }

    public List<ReviewScoreRequest>? Scores { get; set; }
}
