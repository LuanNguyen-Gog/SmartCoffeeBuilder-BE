namespace SmartCoffeeBuilder.Repository.Models;

public class BudgetItem
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string Category { get; set; } = null!;
    public decimal PlannedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
