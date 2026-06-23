namespace SmartCoffeeBuilder.Repository.Models;

public class DesignBrief
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string TargetCustomer { get; set; } = null!;
    public string Style { get; set; } = null!;
    public string Mood { get; set; } = null!;
    public int? SeatCount { get; set; }
    public string? Timeline { get; set; }
    public string? BrandNote { get; set; }
    public string? BusinessModel { get; set; }
    public string? BusinessGoals { get; set; }
    public string? OperationNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<AiRecommendation> AiRecommendations { get; set; } = new List<AiRecommendation>();
}
