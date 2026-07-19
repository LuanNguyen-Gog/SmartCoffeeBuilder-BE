using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.DesignBrief;

public class CreateDesignBriefRequest
{
    [Required]
    public long ProjectShopOwnerId { get; set; }

    [Required]
    public string TargetCustomer { get; set; } = null!;

    [Required]
    public string Style { get; set; } = null!;

    [Required]
    public string Mood { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int? SeatCount { get; set; }

    public string? Timeline { get; set; }
    public string? BrandNote { get; set; }
    public string? BusinessModel { get; set; }
    public string? BusinessGoals { get; set; }
    public string? OperationNote { get; set; }
}
