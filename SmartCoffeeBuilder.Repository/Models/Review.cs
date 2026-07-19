namespace SmartCoffeeBuilder.Repository.Models;

public class Review
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    public decimal OverallRating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public ICollection<ReviewScore> ReviewScores { get; set; } = new List<ReviewScore>();
}
