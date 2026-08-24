namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một tầng của mặt bằng (review 1.1: "… và tầng"). Diện tích mỗi tầng khác nhau là chuyện thường
/// (tầng trệt lùi vào để xe, tầng trên đua ra ban công), nên không suy được từ tổng diện tích chia
/// số tầng.
/// </summary>
public class SiteFloor
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>site_profiles.id</c>, cascade theo hồ sơ mặt bằng.</summary>
    public Guid SiteProfileId { get; set; }

    /// <summary>
    /// Số hiệu tầng: 1 = trệt, 2 = lầu 1… Số ÂM cho hầm (-1 = hầm B1), 0 dành cho gác lửng.
    /// Unique cùng <see cref="SiteProfileId"/> — một mặt bằng không có hai "tầng 2".
    /// </summary>
    public int FloorNo { get; set; }

    /// <summary>Tên hiển thị: "Trệt", "Lầu 1", "Gác lửng", "Sân thượng".</summary>
    public string? Name { get; set; }

    /// <summary>Diện tích sàn của riêng tầng này (m²).</summary>
    public decimal? AreaM2 { get; set; }

    /// <summary>Chiều cao thông thuỷ của tầng này (m) — có thể khác mức trung bình của cả nhà.</summary>
    public decimal? CeilingHeightM { get; set; }

    /// <summary>Công năng dự kiến: khu pha chế, chỗ ngồi, kho, WC, văn phòng…</summary>
    public string? Purpose { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public SiteProfile SiteProfile { get; set; } = null!;

    /// <summary>Các ô cửa/ban công nằm trên tầng này.</summary>
    public ICollection<SiteOpening> Openings { get; set; } = new List<SiteOpening>();
}
