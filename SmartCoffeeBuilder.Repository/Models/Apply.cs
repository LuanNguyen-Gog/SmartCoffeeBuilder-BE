using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Apply
{
    public Guid Id { get; set; }
    public Guid PostId { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string Proposal { get; set; } = null!;
    public int? EstimatedDurationDays { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.pending;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Post Post { get; set; } = null!;
    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
    public ICollection<ProjectWorking> ProjectWorkings { get; set; } = new List<ProjectWorking>();
}
