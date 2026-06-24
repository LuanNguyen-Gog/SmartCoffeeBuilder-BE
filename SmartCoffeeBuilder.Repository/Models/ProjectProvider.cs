using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Trục trung tâm: engagement giữa project và provider (design/construction/both).</summary>
public class ProjectProvider
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public long ProviderId { get; set; }
    /// <summary>nullable = thuê trực tiếp; có giá trị = qua marketplace.</summary>
    public long? ApplicationId { get; set; }
    public ServiceKind ContractType { get; set; }
    public ProviderStatus Status { get; set; } = ProviderStatus.requested;
    public string? RequestMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ServiceProvider Provider { get; set; } = null!;
    public ProjectApplication? Application { get; set; }

    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    public ICollection<Design> Designs { get; set; } = new List<Design>();
    public ICollection<ConstructionItem> ConstructionItems { get; set; } = new List<ConstructionItem>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Doc> Docs { get; set; } = new List<Doc>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
