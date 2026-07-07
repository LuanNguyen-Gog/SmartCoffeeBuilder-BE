using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Project;

public class CreateProjectRequest
{
    [Required]
    public long OwnerId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Address { get; set; } = null!;

    [Range(0, double.MaxValue)]
    public decimal AreaM2 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Budget { get; set; }
}
