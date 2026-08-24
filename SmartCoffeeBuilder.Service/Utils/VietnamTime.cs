namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Mốc NGÀY theo giờ Việt Nam, dùng cho các luật nghiệp vụ đếm bằng ngày (nhật ký thi công, cảnh
/// báo trễ tiến độ). KHÔNG dùng cho các cột thời điểm: <c>created_at</c> / <c>updated_at</c> và
/// mọi so sánh timestamp vẫn giữ nguyên UTC.
///
/// Dùng offset cứng +7 thay vì tra <see cref="TimeZoneInfo"/>: Việt Nam bỏ giờ mùa hè từ 1975 nên
/// offset không bao giờ đổi, trong khi id múi giờ lại khác nhau giữa Windows
/// ("SE Asia Standard Time") và container Linux ("Asia/Ho_Chi_Minh") — tra id là thêm một chỗ
/// hỏng lúc deploy mà không đổi lại được gì.
/// </summary>
public static class VietnamTime
{
    /// <summary>Chênh lệch cố định so với UTC (UTC+7, không có DST).</summary>
    public static readonly TimeSpan Offset = TimeSpan.FromHours(7);

    /// <summary>Thời điểm hiện tại theo giờ VN.</summary>
    public static DateTime Now => DateTime.UtcNow + Offset;

    /// <summary>
    /// Ngày hôm nay theo giờ VN. UTC đi SAU giờ VN 7 tiếng, nên trong khoảng 00:00–07:00 giờ VN,
    /// <c>DateOnly.FromDateTime(DateTime.UtcNow)</c> vẫn còn là HÔM QUA — lấy mốc đó để chặn
    /// "ngày tương lai" sẽ đá nhầm đúng cái ngày người dùng đang sống.
    /// </summary>
    public static DateOnly Today => DateOnly.FromDateTime(Now);
}
