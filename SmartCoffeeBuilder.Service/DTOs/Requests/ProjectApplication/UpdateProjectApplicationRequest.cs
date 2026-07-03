using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectApplication;

/// <summary>Chỉ sửa được khi application còn pending.</summary>
public class UpdateProjectApplicationRequest
{
    public string? Proposal { get; set; }

    [Range(1, int.MaxValue)]
    public int? EstimatedDurationDays { get; set; }
}
