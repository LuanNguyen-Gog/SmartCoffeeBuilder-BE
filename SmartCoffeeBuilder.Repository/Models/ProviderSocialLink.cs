using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một kênh thương hiệu của nhà cung cấp: fanpage, Instagram, TikTok, website… (review 1.1:
/// "mở rộng hồ sơ provider bằng … thương hiệu").
///
/// Bảng riêng thay vì vài cột phẳng trên <see cref="ServiceProviderProfile"/>: số kênh mỗi provider
/// dùng rất khác nhau, và thêm một nền tảng mới thì chỉ là thêm giá trị enum chứ không phải
/// migration đổi cấu trúc bảng.
/// </summary>
public class ProviderSocialLink
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>service_providers.id</c>, cascade theo hồ sơ.</summary>
    public Guid ServiceProviderProfileId { get; set; }

    public SocialPlatform Platform { get; set; }

    /// <summary>URL đầy đủ của kênh.</summary>
    public string Url { get; set; } = null!;

    /// <summary>Nhãn hiển thị tuỳ chọn (tên fanpage…). Bỏ trống thì FE hiện tên nền tảng.</summary>
    public string? Label { get; set; }

    /// <summary>Thứ tự hiển thị — id là uuid nên KHÔNG suy ra được thứ tự nhập.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
}
