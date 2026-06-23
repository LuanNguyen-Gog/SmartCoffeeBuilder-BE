namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Gắn provider + project (owner suy qua project.owner_id), KHÔNG qua project_provider.</summary>
public class Review
{
    public long Id { get; set; }
    public long ProviderId { get; set; }
    public long ProjectId { get; set; }
    public decimal OverallRating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ServiceProvider Provider { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ICollection<ReviewScore> ReviewScores { get; set; } = new List<ReviewScore>();
}
