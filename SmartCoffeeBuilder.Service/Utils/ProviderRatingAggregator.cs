using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Gộp review của một provider thành các con số dùng để vẽ trang profile:
/// tổng số review, điểm overall trung bình, và điểm trung bình theo từng tiêu chí
/// (<c>progress</c> / <c>quality</c> / <c>communication</c> / <c>cost</c> / <c>professionalism</c>).
///
/// Tách ra khỏi <c>ReviewService</c> có chủ đích:
/// <list type="bullet">
/// <item>Tái sử dụng khi nhúng rating vào <c>ServiceProviderProfileResponse</c> /
/// <c>ProviderBrandResponse</c> — tránh hai service cùng <c>GroupBy + Average</c> rồi trôi lệch
/// khi có ai đó sửa một bên quên sửa bên kia.</item>
/// <item>Caller tự chịu trách nhiệm nạp <c>ReviewScores</c> qua <c>Include</c> trước khi gọi —
/// helper không đụng DB.</item>
/// </list>
///
/// Trả về <c>decimal?</c> cho overall vì một provider chưa có review nào sẽ có
/// <c>AverageRating = null</c> thay vì <c>0</c> (0 sẽ bị hiểu nhầm là "có review mà toàn 1*").
/// </summary>
public static class ProviderRatingAggregator
{
    /// <param name="reviews">Danh sách review đã <c>Include</c> <c>ReviewScores</c>.</param>
    public static ProviderRatingAggregate Aggregate(IEnumerable<Review> reviews)
    {
        var list = reviews as IList<Review> ?? reviews.ToList();

        var aggregate = new ProviderRatingAggregate
        {
            ReviewCount = list.Count
        };

        if (list.Count == 0) return aggregate;

        aggregate.AverageRating = Math.Round(list.Average(r => r.OverallRating), 2);

        // Group theo dimension — nếu một review không gửi scores thì cũng không đóng góp vào
        // dimension nào, nhưng VẪN đóng góp vào overall (đúng theo thiết kế: overall là một số
        // owner nhập tay, không tự suy từ scores).
        aggregate.DimensionAverages = list
            .SelectMany(r => r.ReviewScores ?? new List<ReviewScore>())
            .GroupBy(s => s.Dimension)
            .ToDictionary(
                g => g.Key.ToString(),
                g => Math.Round((decimal)g.Average(s => s.Score), 2));

        return aggregate;
    }
}

/// <summary>Kết quả gộp rating. Số rỗng khi chưa có review.</summary>
public class ProviderRatingAggregate
{
    public int ReviewCount { get; set; }

    /// <summary>Trung bình <c>OverallRating</c>. Null khi <see cref="ReviewCount"/> = 0.</summary>
    public decimal? AverageRating { get; set; }

    /// <summary>
    /// Điểm trung bình theo từng tiêu chí (chuỗi Dimension → điểm). Rỗng khi chưa có review nào
    /// gửi scores — khác với null, đây là dictionary chứ không phải nullable.
    /// </summary>
    public Dictionary<string, decimal> DimensionAverages { get; set; } = new();
}
