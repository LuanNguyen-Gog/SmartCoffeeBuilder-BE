using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Design
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public string? Title { get; set; }
    public decimal Version { get; set; } // decimal(4,1)
    public DesignType Type { get; set; }
    public string? Reason { get; set; }
    public DesignStatus Status { get; set; } = DesignStatus.in_progress;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
    public ICollection<DesignImage> DesignImages { get; set; } = new List<DesignImage>();
}
