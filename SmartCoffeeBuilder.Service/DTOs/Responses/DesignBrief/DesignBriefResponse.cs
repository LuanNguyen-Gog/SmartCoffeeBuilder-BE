using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;

public class DesignBriefResponse
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string TargetCustomer { get; set; } = null!;
    public string Style { get; set; } = null!;
    public string Mood { get; set; } = null!;
    public int? SeatCount { get; set; }
    public string? Timeline { get; set; }
    public string? BrandNote { get; set; }
    public string? BusinessModel { get; set; }
    public string? BusinessGoals { get; set; }
    public string? OperationNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static DesignBriefResponse From(SmartCoffeeBuilder.Repository.Models.DesignBrief b) => new()
    {
        Id = b.Id,
        ProjectId = b.ProjectId,
        TargetCustomer = b.TargetCustomer,
        Style = b.Style,
        Mood = b.Mood,
        SeatCount = b.SeatCount,
        Timeline = b.Timeline,
        BrandNote = b.BrandNote,
        BusinessModel = b.BusinessModel,
        BusinessGoals = b.BusinessGoals,
        OperationNote = b.OperationNote,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}
