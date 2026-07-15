namespace SmartCoffeeBuilder.Repository.Models;

public class ShopOwner
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string FullName { get; set; } = null!;
    public string ShopName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public ICollection<ProjectShopOwner> ProjectShopOwners { get; set; } = new List<ProjectShopOwner>();
}
