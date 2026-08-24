using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Thông số VẬT LÝ thật của mặt bằng, 1-1 với <see cref="ProjectShopOwner"/>
/// (review 1.1: "gắn thông số thực tế như kích thước, hướng, cửa, ban công và tầng").
///
/// Tách khỏi <c>projects</c> thay vì nhồi thêm cột phẳng vì hai lý do:
/// <list type="bullet">
/// <item>Tầng và ô cửa là quan hệ 1-n thật (<see cref="SiteFloor"/>, <see cref="SiteOpening"/>) —
/// nhồi vào cột phẳng thì nhà 3 tầng 5 cửa không mô tả nổi.</item>
/// <item><c>projects.area_m2</c> là con số owner tự khai lúc lập dự án; bảng này là hồ sơ kỹ thuật
/// điền dần (có thể sau khảo sát). Trộn chung thì không phân biệt được cái nào đã đo thật.</item>
/// </list>
///
/// Đây cũng là nguồn cho <c>AiRecommendationService</c>: trước đây <c>FloorCount</c> bị hardcode = 1
/// và mặt tiền không gửi đi, nên AI thiết kế mà không biết mặt bằng thật ra sao.
/// </summary>
public class SiteProfile
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>projects.id</c>, UNIQUE (1-1), cascade theo dự án.</summary>
    public Guid ProjectShopOwnerId { get; set; }

    /// <summary>Chiều sâu lô đất (m) — chiều chạy từ mặt tiền vào trong.</summary>
    public decimal? LengthM { get; set; }

    /// <summary>Chiều ngang lô đất (m).</summary>
    public decimal? WidthM { get; set; }

    /// <summary>
    /// Bề rộng MẶT TIỀN (m). Thường bằng <see cref="WidthM"/> nhưng không phải luôn: lô méo, lô góc
    /// hoặc nhà lùi vào trong thì mặt tiền hẹp hơn chiều ngang thật.
    /// </summary>
    public decimal? FrontageWidthM { get; set; }

    /// <summary>Chiều cao thông thuỷ trung bình (m) — quyết định được phép làm gác lửng hay không.</summary>
    public decimal? CeilingHeightM { get; set; }

    /// <summary>Bề rộng đường trước mặt bằng (m) — ảnh hưởng chỗ đỗ xe và khả năng nhận diện.</summary>
    public decimal? RoadWidthM { get; set; }

    /// <summary>Hướng mặt tiền. null = chưa xác định.</summary>
    public Orientation? Orientation { get; set; }

    /// <summary>
    /// Tổng số tầng sử dụng, kể cả trệt. Là con số TỔNG HỢP để tra nhanh và đẩy sang AI;
    /// chi tiết từng tầng nằm ở <see cref="Floors"/>.
    /// </summary>
    public int? FloorCount { get; set; }

    /// <summary>Có gác lửng hay không — ở VN đây là thứ đổi hẳn cách bố trí chỗ ngồi.</summary>
    public bool HasMezzanine { get; set; }

    /// <summary>Kết cấu hiện trạng: nhà phố, shophouse, nhà cấp 4, mặt bằng thô…</summary>
    public string? StructureNote { get; set; }

    /// <summary>Hiện trạng bàn giao: đã có gì, phải đập bỏ gì.</summary>
    public string? ExistingConditionNote { get; set; }

    /// <summary>Account id người điền hồ sơ (owner hoặc provider đi khảo sát).</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectShopOwner ProjectShopOwner { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }

    public ICollection<SiteFloor> Floors { get; set; } = new List<SiteFloor>();
    public ICollection<SiteOpening> Openings { get; set; } = new List<SiteOpening>();
}
