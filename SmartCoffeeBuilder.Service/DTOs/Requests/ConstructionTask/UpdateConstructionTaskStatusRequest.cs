using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;

public class UpdateConstructionTaskStatusRequest
{
    /// <summary>pending | in_progress | completed — chỉ tiến tới, không lùi.</summary>
    [Required]
    public string Status { get; set; } = null!;
}
