using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Project;

public class ProjectResponse
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal AreaM2 { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ProjectProviderSummary> Providers { get; set; } = new();

    public static ProjectResponse From(SmartCoffeeBuilder.Repository.Models.Project p) => new()
    {
        Id = p.Id,
        OwnerId = p.OwnerId,
        Name = p.Name,
        Address = p.Address,
        AreaM2 = p.AreaM2,
        Budget = p.Budget,
        Status = p.Status.ToString(),
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Providers = p.ProjectProviders?
            .Select(ProjectProviderSummary.From)
            .ToList() ?? new()
    };
}
