namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionTask;

/// <summary>Cập nhật thông tin task. Chuyển trạng thái dùng endpoint riêng (/status).</summary>
public class UpdateConstructionTaskRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public DateOnly? StartAt { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualStartAt { get; set; }
    public DateOnly? ActualAt { get; set; }

    /// <summary>Chi phí nhân công dự tính.</summary>
    public decimal? EstimatedLaborCost { get; set; }

    /// <summary>Chi phí nhân công thực chi.</summary>
    public decimal? ActualLaborCost { get; set; }
    /// <summary>Lý do (ví dụ trễ tiến độ).</summary>
    public string? Reason { get; set; }
}
