namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Ràng buộc lịch dùng chung cho hai cấp thi công: milestone (<c>construction_item</c>) và
/// task (<c>construction_task</c>). Hai bảng có cùng cột <c>estimate_at</c> nên cùng một luật.
/// </summary>
public static class ConstructionSchedule
{
    /// <summary>
    /// Hạn hoàn thành (<c>estimate_at</c>) không được đặt về quá khứ — hạng mục lập ra là để làm
    /// sắp tới, đặt hạn đã qua thì mọi báo cáo trễ tiến độ mất ý nghĩa. Hôm nay VẪN hợp lệ
    /// (chỉ chặn TRƯỚC ngày hiện tại); <c>null</c> = không đặt hạn, bỏ qua.
    ///
    /// Mốc so sánh lấy theo GIỜ VN (<see cref="VietnamTime.Today"/>) cho khớp <c>ActualAt</c> /
    /// <c>ActualStartAt</c> ở tầng service. Lấy theo UTC thì trong khoảng 00:00–07:00 giờ VN mốc
    /// còn là HÔM QUA, và người dùng đặt hạn đúng vào ngày hôm qua vẫn lọt qua.
    /// </summary>
    /// <param name="subject">Tên đối tượng để ghép vào câu lỗi, vd "the task" / "the construction item".</param>
    /// <exception cref="ArgumentException">Hạn nằm trước ngày hiện tại (HTTP 400).</exception>
    public static void EnsureEstimateNotInPast(DateOnly? estimateAt, string subject)
    {
        if (estimateAt is not DateOnly due) return;

        var today = VietnamTime.Today;
        if (due < today)
            throw new ArgumentException(
                $"EstimateAt '{due:yyyy-MM-dd}' falls before the current date ({today:yyyy-MM-dd}) — " +
                $"the completion deadline for {subject} cannot be set in the past.");
    }

    /// <summary>
    /// Ngày bắt đầu không được nằm SAU hạn hoàn thành — thời lượng âm là dữ liệu sai, không phải
    /// một lịch chặt. Bỏ qua khi một trong hai mốc còn trống (kế hoạch điền dần).
    /// </summary>
    /// <exception cref="ArgumentException">StartAt > EstimateAt (HTTP 400).</exception>
    public static void EnsureRangeOrdered(DateOnly? startAt, DateOnly? estimateAt, string subject)
    {
        if (startAt is not DateOnly start || estimateAt is not DateOnly due) return;

        if (start > due)
            throw new ArgumentException(
                $"StartAt '{start:yyyy-MM-dd}' falls after EstimateAt '{due:yyyy-MM-dd}' — " +
                $"the duration of {subject} cannot be negative.");
    }

    /// <summary>Số ngày theo kế hoạch, tính cả ngày đầu và ngày cuối. null khi thiếu một mốc.</summary>
    public static int? DurationDays(DateOnly? startAt, DateOnly? endAt) =>
        startAt is DateOnly s && endAt is DateOnly e && e >= s
            ? e.DayNumber - s.DayNumber + 1
            : null;
}
