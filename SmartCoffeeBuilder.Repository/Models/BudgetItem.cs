namespace SmartCoffeeBuilder.Repository.Models;

public class BudgetItem
{
    public long Id { get; set; }
    public long ProjectShopOwnerId { get; set; }
    public string Category { get; set; } = null!;
    public decimal PlannedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectShopOwner ProjectShopOwner { get; set; } = null!;
}
