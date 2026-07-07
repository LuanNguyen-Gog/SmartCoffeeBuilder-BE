using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Project
{
    public long Id { get; set; }
    public long OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal AreaM2 { get; set; }
    public decimal Budget { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.briefed;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ShopOwner Owner { get; set; } = null!;
    public DesignBrief? DesignBrief { get; set; }
    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
    public ICollection<ProjectPost> ProjectPosts { get; set; } = new List<ProjectPost>();
    public ICollection<ProjectProvider> ProjectProviders { get; set; } = new List<ProjectProvider>();
}
