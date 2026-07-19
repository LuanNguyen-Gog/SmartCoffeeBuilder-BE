using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Payment;

public class SubscriptionResponse
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public long PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PaidAmount { get; set; }

    public static SubscriptionResponse From(SmartCoffeeBuilder.Repository.Models.Subscription e) => new()
    {
        Id = e.Id,
        AccountId = e.AccountId,
        PlanId = e.PlanId,
        PlanName = e.Plan.Name,
        Status = e.Status,
        StartDate = e.StartDate,
        EndDate = e.EndDate,
        PaidAmount = e.PaidAmount
    };
}
