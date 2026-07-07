using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectProvider;

public class UpdateProjectProviderStatusRequest
{
    /// <summary>accepted | rejected | designing | designed | constructing | constructed | completed | terminated</summary>
    [Required]
    public string Status { get; set; } = null!;
}
