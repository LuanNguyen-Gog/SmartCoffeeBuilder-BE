using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Apply;

/// <summary>Provider không gửi id của mình — service tự tra ServiceProviderProfile từ account đang đăng nhập.</summary>
public class CreateApplyRequest
{
    [Required]
    public Guid PostId { get; set; }

    [Required]
    public string Proposal { get; set; } = null!;

    [Range(1, int.MaxValue)]
    public int? EstimatedDurationDays { get; set; }
}
