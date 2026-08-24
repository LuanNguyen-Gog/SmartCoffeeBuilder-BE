using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một ô mở trên mặt bằng: cửa chính, cửa phụ, cửa sổ, ban công, sân thượng, giếng trời
/// (review 1.1: "cửa, ban công").
///
/// Neo vào <see cref="SiteProfile"/>; <see cref="SiteFloorId"/> là TUỲ CHỌN — biết cửa nằm ở tầng
/// nào thì gắn, chưa biết thì để null và nó vẫn được tính vào hồ sơ mặt bằng.
/// </summary>
public class SiteOpening
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>site_profiles.id</c>, cascade theo hồ sơ mặt bằng.</summary>
    public Guid SiteProfileId { get; set; }

    /// <summary>FK -> <c>site_floors.id</c>. null = chưa gán tầng cụ thể (FK SET NULL khi xoá tầng).</summary>
    public Guid? SiteFloorId { get; set; }

    public SiteOpeningType Type { get; set; }

    /// <summary>Hướng của ô mở này — có thể khác hướng mặt tiền (cửa hông, ban công sau).</summary>
    public Orientation? Orientation { get; set; }

    public decimal? WidthM { get; set; }
    public decimal? HeightM { get; set; }

    /// <summary>Số lượng ô giống nhau gộp thành một dòng (4 cửa sổ cùng quy cách = 1 dòng, quantity 4).</summary>
    public int Quantity { get; set; } = 1;

    public string? Note { get; set; }

    /// <summary>Thứ tự hiển thị — id là uuid nên KHÔNG suy ra được thứ tự nhập.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SiteProfile SiteProfile { get; set; } = null!;
    public SiteFloor? SiteFloor { get; set; }
}
