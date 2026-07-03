namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectProvider;

public class ProjectProviderResponse
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public long ProviderId { get; set; }
    public string? ProviderDisplayName { get; set; }
    /// <summary>null = thuê trực tiếp; có giá trị = qua marketplace.</summary>
    public long? ApplicationId { get; set; }
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? RequestMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ProjectProviderResponse From(SmartCoffeeBuilder.Repository.Models.ProjectProvider e) => new()
    {
        Id = e.Id,
        ProjectId = e.ProjectId,
        ProjectName = e.Project?.Name,
        ProviderId = e.ProviderId,
        ProviderDisplayName = e.Provider?.DisplayName,
        ApplicationId = e.ApplicationId,
        ContractType = e.ContractType.ToString(),
        Status = e.Status.ToString(),
        RequestMessage = e.RequestMessage,
        StartedAt = e.StartedAt,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
