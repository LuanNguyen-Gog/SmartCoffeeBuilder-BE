namespace SmartCoffeeBuilder.Service.DTOs.Requests.Issue;

/// <summary>Cập nhật thông tin issue. Chuyển trạng thái dùng endpoint riêng (/status).</summary>
public class UpdateIssueRequest
{
    public Guid? IssueTypeId { get; set; }
    public string? Cause { get; set; }
    public string? Reason { get; set; }
    public string? Solution { get; set; }
    public string? IssueImage { get; set; }
    public string? ConfirmImage { get; set; }
    public DateOnly? EstimateAt { get; set; }
}
