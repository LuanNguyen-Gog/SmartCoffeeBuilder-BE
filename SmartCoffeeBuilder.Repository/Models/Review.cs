namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Đánh giá của chủ quán về nhà cung cấp sau khi engagement được nghiệm thu.
/// Neo vào <see cref="ProjectWorking"/> — provider và dự án suy ra gián tiếp qua đó.
/// </summary>
public class Review
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public decimal OverallRating { get; set; }
    public string? Comment { get; set; }

    /// <summary>
    /// Phản hồi CÔNG KHAI của nhà cung cấp cho đánh giá này (review 1.1: hoàn thiện thông tin
    /// đánh giá). Một chiều: mỗi review đúng MỘT phản hồi, provider sửa được nhưng owner không
    /// trả lời tiếp — đây là quyền đáp lời, không phải một thread tranh luận (thread đã có
    /// <see cref="Conversation"/>).
    /// </summary>
    public string? ProviderReply { get; set; }

    /// <summary>Mốc provider trả lời gần nhất. null = chưa phản hồi.</summary>
    public DateTime? RepliedAt { get; set; }

    /// <summary>Account id người trả lời — lấy từ JWT, không nhận từ body.</summary>
    public Guid? RepliedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account? RepliedByAccount { get; set; }
    public ICollection<ReviewScore> ReviewScores { get; set; } = new List<ReviewScore>();

    /// <summary>Ảnh thành phẩm chủ quán đính kèm (review 1.1).</summary>
    public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
}
