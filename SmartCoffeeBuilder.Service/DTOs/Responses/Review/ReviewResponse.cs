using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Review;

public class ReviewScoreResponse
{
    public Guid Id { get; set; }
    public string Dimension { get; set; } = null!;
    public int Score { get; set; }

    public static ReviewScoreResponse From(SmartCoffeeBuilder.Repository.Models.ReviewScore s) => new()
    {
        Id = s.Id,
        Dimension = s.Dimension.ToString(),
        Score = s.Score
    };
}

public class ReviewResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ProjectShopOwnerId { get; set; }
    public Guid? ServiceProviderProfileId { get; set; }
    public decimal OverallRating { get; set; }
    public string? Comment { get; set; }
    public List<ReviewScoreResponse> Scores { get; set; } = new();

    /// <summary>Phản hồi công khai của nhà cung cấp. null = chưa trả lời (review 1.1).</summary>
    public string? ProviderReply { get; set; }
    public DateTime? RepliedAt { get; set; }

    /// <summary>Ảnh thành phẩm chủ quán đính kèm.</summary>
    public List<ReviewImageResponse> Images { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ReviewResponse From(SmartCoffeeBuilder.Repository.Models.Review r) => new()
    {
        Id = r.Id,
        ProjectWorkingId = r.ProjectWorkingId,
        ProjectShopOwnerId = r.ProjectWorking?.ProjectShopOwnerId,
        ServiceProviderProfileId = r.ProjectWorking?.ServiceProviderProfileId,
        OverallRating = r.OverallRating,
        Comment = r.Comment,
        Scores = r.ReviewScores?.Select(ReviewScoreResponse.From).ToList() ?? new(),
        ProviderReply = r.ProviderReply,
        RepliedAt = r.RepliedAt,
        Images = r.Images?.OrderBy(i => i.SortOrder).Select(ReviewImageResponse.From).ToList() ?? new(),
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}

/// <summary>Một ảnh đính kèm đánh giá (review 1.1).</summary>
public class ReviewImageResponse
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public string ImageUrl { get; set; } = null!;

    /// <summary>URL public tuyệt đối — FE dùng thẳng làm img src.</summary>
    public string? ImageViewUrl { get; set; }

    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ReviewImageResponse From(SmartCoffeeBuilder.Repository.Models.ReviewImage e) => new()
    {
        Id = e.Id,
        ReviewId = e.ReviewId,
        ImageUrl = e.ImageUrl,
        ImageViewUrl = MediaUrl.Resolve(e.ImageUrl),
        Caption = e.Caption,
        SortOrder = e.SortOrder,
        CreatedAt = e.CreatedAt
    };
}
