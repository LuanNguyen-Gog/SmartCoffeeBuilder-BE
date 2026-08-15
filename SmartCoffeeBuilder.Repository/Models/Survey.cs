namespace SmartCoffeeBuilder.Repository.Models;

public class Survey
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    // Bỏ cột Version: survey là bản ghi khảo sát độc lập, xếp theo CreatedAt là đủ —
    // không có nghiệp vụ nào đọc số hiệu phiên bản (khác Design/DesignVersion).
    public string ConditionNote { get; set; } = null!;
    public string? ReportUrl { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
}
