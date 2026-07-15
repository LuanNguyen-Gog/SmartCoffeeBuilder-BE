using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Apply;

/// <summary>Chỉ sửa được khi application còn pending.</summary>
public class UpdateApplyRequest
{
    public string? Proposal { get; set; }

    [Range(1, int.MaxValue)]
    public int? EstimatedDurationDays { get; set; }
}
