namespace SmartCoffeeBuilder.Repository.Models;

public class Survey
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    public decimal Version { get; set; } // decimal(4,1), vd 0.1, 1.4
    public string ConditionNote { get; set; } = null!;
    public string? ReportUrl { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
}
