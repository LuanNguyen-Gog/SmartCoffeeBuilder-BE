using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một DỰ ÁN MẪU trong hồ sơ năng lực của nhà cung cấp (review 1.1: "mở rộng hồ sơ provider bằng
/// dự án mẫu, video, năng lực, thương hiệu").
///
/// Trước đây năng lực chỉ có đúng một chuỗi <c>service_providers.portfolio_headline</c>, nên chủ
/// quán không có gì để so sánh giữa các provider cùng ứng tuyển. <c>docs</c> KHÔNG thay được vai
/// trò này: nó neo vào <c>project_providers</c> nên phải có hợp tác rồi mới up được file, còn dự án
/// mẫu là thứ phải xem TRƯỚC khi quyết định thuê.
///
/// Dự án mẫu là hồ sơ tự khai và KHÔNG liên kết với <c>projects</c> trong hệ thống — phần lớn công
/// trình cũ của provider diễn ra ngoài nền tảng.
/// </summary>
public class ProviderPortfolio
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>service_providers.id</c>, cascade theo hồ sơ provider.</summary>
    public Guid ServiceProviderProfileId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Vai trò provider đảm nhận ở công trình đó: thiết kế, thi công, hay trọn gói.</summary>
    public ServiceKind Role { get; set; } = ServiceKind.both;

    /// <summary>Phong cách: industrial, minimal, vintage… — để chủ quán lọc theo gu của mình.</summary>
    public string? Style { get; set; }

    /// <summary>Địa điểm công trình (tỉnh/thành hoặc địa chỉ rút gọn).</summary>
    public string? Location { get; set; }

    /// <summary>Quy mô mặt bằng (m²).</summary>
    public decimal? AreaM2 { get; set; }

    /// <summary>Giá trị hợp đồng của công trình đó — provider tự khai, chỉ để tham khảo quy mô.</summary>
    public decimal? ContractValue { get; set; }

    /// <summary>Thời điểm hoàn thành — dùng để sắp theo độ mới và tính "kinh nghiệm gần đây".</summary>
    public DateOnly? CompletedAt { get; set; }

    /// <summary>Số ngày thi công thực tế của công trình đó.</summary>
    public int? DurationDays { get; set; }

    /// <summary>
    /// Video giới thiệu công trình (link YouTube hoặc ObjectName trên bucket GCS). Review 1.1 nêu
    /// đích danh "video" — ảnh tĩnh không cho thấy được không gian thật khi đi qua.
    /// </summary>
    public string? VideoUrl { get; set; }

    /// <summary>ObjectName ảnh bìa — ảnh hiển thị ở danh sách, tránh phải nạp cả bộ ảnh.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Ghim lên đầu hồ sơ — provider chọn vài công trình tiêu biểu.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Thứ tự hiển thị — id là uuid nên KHÔNG suy ra được thứ tự nhập.</summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
    public ICollection<ProviderPortfolioImage> Images { get; set; } = new List<ProviderPortfolioImage>();
}
