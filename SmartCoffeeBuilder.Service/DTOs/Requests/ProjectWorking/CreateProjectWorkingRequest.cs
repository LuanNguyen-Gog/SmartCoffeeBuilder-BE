using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;

/// <summary>Thuê trực tiếp (không qua marketplace) — engagement tạo với status=requested, chờ provider phản hồi.</summary>
public class CreateProjectWorkingRequest
{
    [Required]
    public Guid ProjectShopOwnerId { get; set; }

    [Required]
    public Guid ServiceProviderProfileId { get; set; }

    /// <summary>design | construction | both</summary>
    [Required]
    public string ContractType { get; set; } = null!;

    public string? RequestMessage { get; set; }
}
