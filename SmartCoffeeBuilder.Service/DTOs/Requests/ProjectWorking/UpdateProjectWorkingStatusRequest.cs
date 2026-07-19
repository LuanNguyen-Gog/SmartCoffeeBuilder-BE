using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;

public class UpdateProjectWorkingStatusRequest
{
    /// <summary>accepted | rejected | completed | terminated</summary>
    [Required]
    public string Status { get; set; } = null!;
}
