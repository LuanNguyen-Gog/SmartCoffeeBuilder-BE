using SmartCoffeeBuilder.Repository.Models;
using ShopOwnerModel = SmartCoffeeBuilder.Repository.Models.ShopOwner;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Project;

/// <summary>
/// Thông tin rút gọn của shop owner đính kèm trong <see cref="ProjectResponse"/>.
/// </summary>
public class ShopOwnerSummary
{
    public long Id { get; set; }
    public string FullName { get; set; } = null!;
    public string ShopName { get; set; } = null!;
    public string Phone { get; set; } = null!;

    public static ShopOwnerSummary From(ShopOwnerModel s) => new()
    {
        Id = s.Id,
        FullName = s.FullName,
        ShopName = s.ShopName,
        Phone = s.Phone
    };
}