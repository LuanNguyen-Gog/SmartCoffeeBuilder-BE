using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ProjectPost
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public ServiceKind ServiceKind { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public PostStatus Status { get; set; } = PostStatus.open;
    public DateTime? SubmissionDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ICollection<ProjectApplication> ProjectApplications { get; set; } = new List<ProjectApplication>();
}
