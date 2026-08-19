namespace SmartCoffeeBuilder.Repository.Models;

public class ReviewScore
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public string Dimension { get; set; } = null!;
    public int Score { get; set; }

    public Review Review { get; set; } = null!;
}
