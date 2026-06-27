using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ShopOwner;

public class ShopOwnerResponse
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string FullName { get; set; } = null!;
    public string ShopName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Address { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ShopOwnerResponse From(SmartCoffeeBuilder.Repository.Models.ShopOwner s) => new()
    {
        Id = s.Id,
        AccountId = s.AccountId,
        FullName = s.FullName,
        ShopName = s.ShopName,
        Phone = s.Phone,
        Address = s.Address,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };
}
