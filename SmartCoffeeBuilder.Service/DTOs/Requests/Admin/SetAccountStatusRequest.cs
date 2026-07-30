using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Admin;

/// <summary>Admin đổi trạng thái tài khoản.</summary>
public class SetAccountStatusRequest
{
    /// <summary>active | inactive | banned | pending</summary>
    [Required]
    public string Status { get; set; } = null!;
}
