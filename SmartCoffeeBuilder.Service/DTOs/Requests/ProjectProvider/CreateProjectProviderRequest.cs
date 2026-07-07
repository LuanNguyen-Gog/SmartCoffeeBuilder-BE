using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectProvider;

/// <summary>Thuê trực tiếp (không qua marketplace) — engagement tạo với status=requested, chờ provider phản hồi.</summary>
public class CreateProjectProviderRequest
{
    [Required]
    public long ProjectId { get; set; }

    [Required]
    public long ProviderId { get; set; }

    /// <summary>design | construction | both</summary>
    [Required]
    public string ContractType { get; set; } = null!;

    public string? RequestMessage { get; set; }
}
