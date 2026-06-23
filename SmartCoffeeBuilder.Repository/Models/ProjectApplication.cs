using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ProjectApplication
{
    public long Id { get; set; }
    public long PostId { get; set; }
    public long ProviderId { get; set; }
    public string Proposal { get; set; } = null!;
    public decimal? BidAmount { get; set; }
    public int? EstimatedDurationDays { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.pending;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectPost Post { get; set; } = null!;
    public ServiceProvider Provider { get; set; } = null!;
    public ICollection<ProjectProvider> ProjectProviders { get; set; } = new List<ProjectProvider>();
}
