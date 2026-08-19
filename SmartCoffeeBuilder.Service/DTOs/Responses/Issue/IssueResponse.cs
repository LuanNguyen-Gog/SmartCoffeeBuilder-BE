using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Issue;

public class IssueResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ConstructionItemId { get; set; }
    public Guid IssueTypeId { get; set; }
    public string? IssueTypeName { get; set; }
    public string? Cause { get; set; }
    public string? Reason { get; set; }
    public string? Solution { get; set; }
    /// <summary>ObjectName ảnh hiện trạng lỗi trên bucket — giá trị lưu trong DB.</summary>
    public string? IssueImage { get; set; }
    /// <summary>ObjectName ảnh nghiệm thu sau khắc phục — giá trị lưu trong DB.</summary>
    public string? ConfirmImage { get; set; }
    /// <summary>URL public tuyệt đối của IssueImage — FE dùng thẳng làm img src.</summary>
    public string? IssueImageViewUrl { get; set; }
    /// <summary>URL public tuyệt đối của ConfirmImage — FE dùng thẳng làm img src.</summary>
    public string? ConfirmImageViewUrl { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public string Status { get; set; } = null!;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static IssueResponse From(SmartCoffeeBuilder.Repository.Models.Issue e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        ConstructionItemId = e.ConstructionItemId,
        IssueTypeId = e.IssueTypeId,
        IssueTypeName = e.IssueType?.Name,
        Cause = e.Cause,
        Reason = e.Reason,
        Solution = e.Solution,
        IssueImage = e.IssueImage,
        ConfirmImage = e.ConfirmImage,
        IssueImageViewUrl = MediaUrl.Resolve(e.IssueImage),
        ConfirmImageViewUrl = MediaUrl.Resolve(e.ConfirmImage),
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
