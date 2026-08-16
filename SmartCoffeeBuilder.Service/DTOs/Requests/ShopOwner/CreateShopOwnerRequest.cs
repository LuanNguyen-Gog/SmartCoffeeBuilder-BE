using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ShopOwner;

public class CreateShopOwnerRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    public string ShopName { get; set; } = null!;

    [Required]
    [Phone]
    public string Phone { get; set; } = null!;

    [Required]
    public string Address { get; set; } = null!;
}
