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
    /// Mốc so sánh lấy từ <see cref="DateTime.UtcNow"/> cho khớp phần còn lại của tầng service
    /// (<c>ActualAt</c> cũng vậy). VN là UTC+7 nên mốc này chỉ có thể DỄ hơn giờ địa phương,
    /// không bao giờ chặn nhầm một ngày còn hợp lệ.
    /// </summary>
    /// <param name="subject">Tên đối tượng để ghép vào câu lỗi, vd "task" / "hạng mục".</param>
    /// <exception cref="ArgumentException">Hạn nằm trước ngày hiện tại (HTTP 400).</exception>
    public static void EnsureEstimateNotInPast(DateOnly? estimateAt, string subject)
    {
        if (estimateAt is not DateOnly due) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (due < today)
            throw new ArgumentException(
                $"EstimateAt '{due:yyyy-MM-dd}' nằm trước ngày hiện tại ({today:yyyy-MM-dd}) — " +
                $"hạn hoàn thành {subject} không được đặt về quá khứ.");
    }
}
