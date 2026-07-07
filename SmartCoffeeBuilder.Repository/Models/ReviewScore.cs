namespace SmartCoffeeBuilder.Repository.Models;

public class ReviewScore
{
    public long Id { get; set; }
    public long ReviewId { get; set; }
    public string Dimension { get; set; } = null!;
    public int Score { get; set; }

    public Review Review { get; set; } = null!;
}
