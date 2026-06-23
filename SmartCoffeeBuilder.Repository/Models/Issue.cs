using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Dùng chung cho cả design-phase và construction-phase (neo vào project_provider).</summary>
public class Issue
{
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public long? ConstructionItemId { get; set; } // chỉ set khi gắn hạng mục thi công
    public long IssueTypeId { get; set; }
    public string? Cause { get; set; }
    public string? Reason { get; set; }
    public string? Solution { get; set; }
    public string? IssueImage { get; set; }
    public string? ConfirmImage { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public IssueStatus Status { get; set; } = IssueStatus.open;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectProvider ProjectProvider { get; set; } = null!;
    public ConstructionItem? ConstructionItem { get; set; }
    public IssueType IssueType { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
}
