using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Payment;

public class SubscriptionPlanResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public AccountRole TargetRole { get; set; }
    public decimal Price { get; set; }
    public int DurationInDays { get; set; }

    public static SubscriptionPlanResponse From(SmartCoffeeBuilder.Repository.Models.SubscriptionPlan e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Description = e.Description,
        TargetRole = e.TargetRole,
        Price = e.Price,
        DurationInDays = e.DurationInDays
    };
}
