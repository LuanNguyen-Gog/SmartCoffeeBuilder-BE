using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một file hiện trường đính kèm <see cref="DailyLog"/> (review 3: "Hình ảnh/video hiện trường").
///
/// Tách bảng con thay vì nhồi cột <c>image_url</c> phẳng như <see cref="ConstructionTask"/>: một
/// buổi thi công chụp cả chục ảnh, và ảnh với video cần phân biệt để FE render đúng thẻ
/// (<c>&lt;img&gt;</c> hay player).
/// </summary>
public class DailyLogMedia
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>daily_logs.id</c>, cascade theo nhật ký.</summary>
    public Guid DailyLogId { get; set; }

    /// <summary>ObjectName trên bucket (hoặc URL ngoài) — resolve qua <c>MediaUrl</c> khi trả về FE.</summary>
    public string MediaUrl { get; set; } = null!;

    public DailyLogMediaType MediaType { get; set; } = DailyLogMediaType.image;

    public string? Caption { get; set; }

    /// <summary>Thứ tự hiển thị — id là uuid nên KHÔNG suy ra được thứ tự upload.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DailyLog DailyLog { get; set; } = null!;
}
