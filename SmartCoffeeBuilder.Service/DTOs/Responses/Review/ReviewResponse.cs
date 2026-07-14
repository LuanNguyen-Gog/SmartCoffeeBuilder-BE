namespace SmartCoffeeBuilder.Service.DTOs.Responses.Review;

public class ReviewScoreResponse
{
    public long Id { get; set; }
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
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public long? ProjectId { get; set; }
    public long? ProviderId { get; set; }
    public decimal OverallRating { get; set; }
    public string? Comment { get; set; }
    public List<ReviewScoreResponse> Scores { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ReviewResponse From(SmartCoffeeBuilder.Repository.Models.Review r) => new()
    {
        Id = r.Id,
        ProjectProviderId = r.ProjectProviderId,
        ProjectId = r.ProjectProvider?.ProjectId,
        ProviderId = r.ProjectProvider?.ProviderId,
        OverallRating = r.OverallRating,
        Comment = r.Comment,
        Scores = r.ReviewScores?.Select(ReviewScoreResponse.From).ToList() ?? new(),
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
