namespace SmartCoffeeBuilder.Service.DTOs.Responses.Review;

public class ReviewScoreResponse
{
    public Guid Id { get; set; }
    public string Dimension { get; set; } = null!;
    public int Score { get; set; }

    public static ReviewScoreResponse From(SmartCoffeeBuilder.Repository.Models.ReviewScore s) => new()
    {
        Id = s.Id,
        Dimension = s.Dimension,
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
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
