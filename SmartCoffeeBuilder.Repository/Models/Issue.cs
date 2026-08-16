using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Dùng chung cho cả design-phase và construction-phase (neo vào project_provider).</summary>
public class Issue
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ConstructionItemId { get; set; } // chỉ set khi gắn hạng mục thi công
    public Guid IssueTypeId { get; set; }
    public string? Cause { get; set; }
    public string? Reason { get; set; }
    public string? Solution { get; set; }
    public string? IssueImage { get; set; }
    public string? ConfirmImage { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.open;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public ConstructionItem? ConstructionItem { get; set; }
    public IssueType IssueType { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
}
