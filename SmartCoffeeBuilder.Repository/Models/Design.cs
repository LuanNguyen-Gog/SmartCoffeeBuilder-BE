using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Design
{
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public string? Title { get; set; }
    public decimal Version { get; set; } // decimal(4,1)
    public DesignType Type { get; set; }
    public string? Reason { get; set; }
    public DesignStatus Status { get; set; } = DesignStatus.in_progress;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectProvider ProjectProvider { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
    public ICollection<DesignImage> DesignImages { get; set; } = new List<DesignImage>();
}
