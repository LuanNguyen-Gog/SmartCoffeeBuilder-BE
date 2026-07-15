namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

public class ProjectWorkingResponse
{
    public long Id { get; set; }
    public long ProjectShopOwnerId { get; set; }
    public string? ProjectName { get; set; }
    public long ServiceProviderProfileId { get; set; }
    public string? ProviderDisplayName { get; set; }
    /// <summary>null = thuê trực tiếp; có giá trị = qua marketplace.</summary>
    public long? ApplyId { get; set; }
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? RequestMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ProjectWorkingResponse From(SmartCoffeeBuilder.Repository.Models.ProjectWorking e) => new()
    {
        Id = e.Id,
        ProjectShopOwnerId = e.ProjectShopOwnerId,
        ProjectName = e.ProjectShopOwner?.Name,
        ServiceProviderProfileId = e.ServiceProviderProfileId,
        ProviderDisplayName = e.ServiceProviderProfile?.DisplayName,
        ApplyId = e.ApplyId,
        ContractType = e.ContractType.ToString(),
        Status = e.Status.ToString(),
        RequestMessage = e.RequestMessage,
        StartedAt = e.StartedAt,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
