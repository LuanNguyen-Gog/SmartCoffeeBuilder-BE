using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectApplication;

public class CreateProjectApplicationRequest
{
    [Required]
    public long PostId { get; set; }

    [Required]
    public long ProviderId { get; set; }

    [Required]
    public string Proposal { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int? EstimatedDurationDays { get; set; }
}
