using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Issue;

/// <summary>Tạo issue (dùng chung design/construction), neo vào engagement; tuỳ chọn gắn hạng mục thi công.</summary>
public class CreateIssueRequest
{
    [Required]
    public long ProjectProviderId { get; set; }

    /// <summary>Chỉ set khi gắn với một milestone thi công — phải cùng engagement.</summary>
    public long? ConstructionItemId { get; set; }

    [Required]
    public long IssueTypeId { get; set; }

    public string? Cause { get; set; }
    public string? Reason { get; set; }
    public string? Solution { get; set; }
    public string? IssueImage { get; set; }
    public string? ConfirmImage { get; set; }
    public DateOnly? EstimateAt { get; set; }

    /// <summary>Account id của người tạo.</summary>
    public long? CreatedBy { get; set; }
}
