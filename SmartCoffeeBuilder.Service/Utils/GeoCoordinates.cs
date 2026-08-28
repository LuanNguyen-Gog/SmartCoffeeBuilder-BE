namespace SmartCoffeeBuilder.Service.Utils;

/// <summary>
/// Ràng buộc toạ độ dùng chung cho mọi bảng có địa chỉ kèm vị trí trên bản đồ:
/// <c>projects.latitude/longitude</c> (mặt bằng dự án) và
/// <c>service_providers.company_latitude/company_longitude</c> (văn phòng/xưởng).
///
/// Toạ độ là dữ liệu KÈM THEO địa chỉ, không thay thế nó: người dùng vẫn gõ/sửa được địa chỉ
/// chữ, còn cặp lat/lng chỉ có khi họ chọn từ bản đồ. Vì vậy cả hai cột đều nullable và
/// <see cref="EnsurePairValid"/> chấp nhận trạng thái "chưa có toạ độ".
///
/// Cố ý KHÔNG lưu <c>place_id</c> của Google: lat/lng là dữ liệu phổ quát, đổi nhà cung cấp bản
/// đồ về sau không mất gì; <c>place_id</c> thì chết theo nhà cung cấp.
/// </summary>
public static class GeoCoordinates
{
    /// <summary>Vĩ độ hợp lệ theo WGS 84.</summary>
    private const double MinLatitude = -90d;
    private const double MaxLatitude = 90d;

    /// <summary>Kinh độ hợp lệ theo WGS 84.</summary>
    private const double MinLongitude = -180d;
    private const double MaxLongitude = 180d;

    /// <summary>
    /// Kiểm tra một cặp toạ độ trước khi ghi xuống DB.
    ///
    /// Ba luật:
    /// <list type="number">
    /// <item>Cả hai cùng <c>null</c> — hợp lệ, nghĩa là "địa chỉ chữ, chưa ghim bản đồ".</item>
    /// <item>Chỉ MỘT trong hai có giá trị — sai. Một vĩ độ không kèm kinh độ không chỉ tới đâu cả;
    /// để lọt xuống DB thì mọi chỗ đọc về sau đều phải tự đoán ý.</item>
    /// <item>Ngoài dải WGS 84 — sai.</item>
    /// </list>
    /// </summary>
    /// <param name="subject">Tên đối tượng ghép vào câu lỗi, vd "the project" / "the company address".</param>
    /// <exception cref="ArgumentException">Cặp toạ độ không hợp lệ (HTTP 400).</exception>
    public static void EnsurePairValid(double? latitude, double? longitude, string subject)
    {
        if (latitude is null && longitude is null) return;

        if (latitude is null || longitude is null)
            throw new ArgumentException(
                $"Latitude and longitude for {subject} must be sent together — " +
                $"received {(latitude is null ? "longitude" : "latitude")} only. " +
                "Send both to pin a location, or neither to keep the address as text.");

        var lat = latitude.Value;
        var lng = longitude.Value;

        if (double.IsNaN(lat) || double.IsNaN(lng) || double.IsInfinity(lat) || double.IsInfinity(lng))
            throw new ArgumentException($"Latitude/longitude for {subject} must be finite numbers.");

        if (lat is < MinLatitude or > MaxLatitude)
            throw new ArgumentException(
                $"Latitude {lat} for {subject} is out of range — it must be between {MinLatitude} and {MaxLatitude}.");

        if (lng is < MinLongitude or > MaxLongitude)
            throw new ArgumentException(
                $"Longitude {lng} for {subject} is out of range — it must be between {MinLongitude} and {MaxLongitude}.");

        // Null Island. Toạ độ (0, 0) rơi giữa vịnh Guinea — không có mặt bằng nào ở đó, nên gần
        // như chắc chắn là biến chưa khởi tạo ở client gửi lên (double mặc định = 0) chứ không
        // phải người dùng thật sự ghim vào đấy. Chặn ở đây rẻ hơn nhiều so với việc về sau phải
        // ngồi lọc xem pin nào là thật.
        if (lat == 0d && lng == 0d)
            throw new ArgumentException(
                $"Coordinates (0, 0) for {subject} are almost certainly an uninitialised value, not a real location. " +
                "Send the coordinates the user picked, or omit both fields.");
    }
}
