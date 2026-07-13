using SmartCoffeeBuilder.Repository.Models;
using ProjectProviderModel = SmartCoffeeBuilder.Repository.Models.ProjectProvider;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Project;

/// <summary>
/// Thông tin rút gọn của provider đính kèm trong <see cref="ProjectResponse"/>.
/// Lấy từ ProjectProvider (engagement) để kèm ContractType + Status của quan hệ.
/// </summary>
public class ProjectProviderSummary
{
    public long ProjectProviderId { get; set; }
    public long ProviderId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string ProviderType { get; set; } = null!;
    public string Capability { get; set; } = null!;
    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public static ProjectProviderSummary From(ProjectProviderModel pp) => new()
    {
        ProjectProviderId = pp.Id,
        ProviderId = pp.ProviderId,
        DisplayName = pp.Provider.DisplayName,
        ProviderType = pp.Provider.ProviderType.ToString(),
        Capability = pp.Provider.Capability.ToString(),
        IsVerified = pp.Provider.IsVerified,
        AvgRating = pp.Provider.AvgRating,
        ContractType = pp.ContractType.ToString(),
        Status = pp.Status.ToString(),
        CreatedAt = pp.CreatedAt
    };
}