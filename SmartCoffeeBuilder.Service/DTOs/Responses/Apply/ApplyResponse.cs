namespace SmartCoffeeBuilder.Service.DTOs.Responses.Apply;

public class ApplyResponse
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public string? PostTitle { get; set; }
    public long? ProjectShopOwnerId { get; set; }
    public long ServiceProviderProfileId { get; set; }
    public string? ProviderDisplayName { get; set; }
    public string Proposal { get; set; } = null!;
    public int? EstimatedDurationDays { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ApplyResponse From(SmartCoffeeBuilder.Repository.Models.Apply a) => new()
    {
        Id = a.Id,
        PostId = a.PostId,
        PostTitle = a.Post?.Title,
        ProjectShopOwnerId = a.Post?.ProjectShopOwnerId,
        ServiceProviderProfileId = a.ServiceProviderProfileId,
        ProviderDisplayName = a.ServiceProviderProfile?.DisplayName,
        Proposal = a.Proposal,
        EstimatedDurationDays = a.EstimatedDurationDays,
        Status = a.Status.ToString(),
        SubmittedAt = a.SubmittedAt,
        CreatedAt = a.CreatedAt,
        UpdatedAt = a.UpdatedAt
    };
}
