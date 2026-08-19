using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ReviewScore
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }

    /// <summary>
    /// Tiêu chí được chấm — enum cố định, KHÔNG còn là chuỗi tự do. Xem
    /// <see cref="ReviewDimension"/> để biết vì sao: text tự do làm phần trung bình theo tiêu chí
    /// vỡ thành nhiều dòng gần giống nhau và không so sánh được giữa các provider.
    /// </summary>
    public ReviewDimension Dimension { get; set; }

    public int Score { get; set; }

    public Review Review { get; set; } = null!;
}
