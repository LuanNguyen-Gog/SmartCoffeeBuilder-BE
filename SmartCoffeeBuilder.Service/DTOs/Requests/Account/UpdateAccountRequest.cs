using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Account;

public class UpdateAccountRequest
{
    [Phone]
    public string? Phone { get; set; }

    /// <summary>owner | provider | admin</summary>
    public string? Role { get; set; }

    /// <summary>active | inactive | banned | pending</summary>
    public string? Status { get; set; }
}
