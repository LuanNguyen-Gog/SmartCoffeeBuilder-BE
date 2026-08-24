namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Ảnh chủ quán đính kèm khi đánh giá (review 1.1: hoàn thiện "thông tin đánh giá").
/// Ảnh thành phẩm thật là thứ thuyết phục hơn hẳn điểm số — và nó cũng là bằng chứng khi đánh giá
/// xấu bị tranh cãi. Lưu ObjectName trên bucket GCS như mọi chỗ khác.
/// </summary>
public class ReviewImage
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>reviews.id</c>, cascade theo review.</summary>
    public Guid ReviewId { get; set; }

    /// <summary>ObjectName trên bucket (FE hiển thị bằng URL public do BE resolve).</summary>
    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public Review Review { get; set; } = null!;
}
