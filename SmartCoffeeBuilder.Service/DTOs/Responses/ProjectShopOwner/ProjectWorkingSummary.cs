using SmartCoffeeBuilder.Repository.Models;
using ProjectWorkingModel = SmartCoffeeBuilder.Repository.Models.ProjectWorking;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectShopOwner;

/// <summary>
/// Thông tin rút gọn của provider đính kèm trong <see cref="ProjectShopOwnerResponse"/>.
/// Lấy từ ProjectWorking (engagement) để kèm ContractType + Status của quan hệ.
/// </summary>
public class ProjectWorkingSummary
{
    public long ProjectWorkingId { get; set; }
    public long ServiceProviderProfileId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string ProviderType { get; set; } = null!;
    public string Capability { get; set; } = null!;
    public bool IsVerified { get; set; }
    public decimal AvgRating { get; set; }
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public static ProjectWorkingSummary From(ProjectWorkingModel pp) => new()
    {
        ProjectWorkingId = pp.Id,
        ServiceProviderProfileId = pp.ServiceProviderProfileId,
        DisplayName = pp.ServiceProviderProfile.DisplayName,
        ProviderType = pp.ServiceProviderProfile.ProviderType.ToString(),
        Capability = pp.ServiceProviderProfile.Capability.ToString(),
        IsVerified = pp.ServiceProviderProfile.IsVerified,
        AvgRating = pp.ServiceProviderProfile.AvgRating,
        ContractType = pp.ContractType.ToString(),
        Status = pp.Status.ToString(),
        CreatedAt = pp.CreatedAt
    };
}