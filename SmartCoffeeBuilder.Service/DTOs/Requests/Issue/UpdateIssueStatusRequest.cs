using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Issue;

public class UpdateIssueStatusRequest
{
    /// <summary>open | in_progress | resolved | closed — chỉ tiến tới, không lùi.</summary>
    [Required]
    public string Status { get; set; } = null!;
}
