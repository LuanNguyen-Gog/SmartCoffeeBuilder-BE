namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;

/// <summary>Cập nhật thông tin task. Chuyển trạng thái dùng endpoint riêng (/status).</summary>
public class UpdateConstructionTaskRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? EstimateAt { get; set; }
    /// <summary>Lý do (ví dụ trễ tiến độ).</summary>
    public string? Reason { get; set; }
}
