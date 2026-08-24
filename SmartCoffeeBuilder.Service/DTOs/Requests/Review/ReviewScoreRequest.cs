using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Review;

/// <summary>
/// Điểm cho MỘT tiêu chí đánh giá. <see cref="Dimension"/> phải là một giá trị trong danh sách
/// cố định: <c>progress</c>, <c>quality</c>, <c>communication</c>, <c>cost</c>,
/// <c>professionalism</c> — không còn nhận chuỗi tự do, xem
/// <see cref="SmartCoffeeBuilder.Repository.Models.Enums.ReviewDimension"/>.
/// </summary>
public class ReviewScoreRequest
{
    [Required]
    [MaxLength(30)]
    public string Dimension { get; set; } = null!;

    [Range(1, 5)]
    public int Score { get; set; }
}
