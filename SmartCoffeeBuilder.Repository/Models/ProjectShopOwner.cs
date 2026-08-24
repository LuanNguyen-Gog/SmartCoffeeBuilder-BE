using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ProjectShopOwner
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
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

    /// <summary>Hồ sơ thông số vật lý của mặt bằng (1-1) — kích thước, hướng, tầng, cửa/ban công.</summary>
    public SiteProfile? SiteProfile { get; set; }
    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<ProjectWorking> ProjectWorkings { get; set; } = new List<ProjectWorking>();
}
