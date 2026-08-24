using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionTask;

public class ConstructionTaskResponse
{
    public Guid Id { get; set; }
    public Guid ConstructionItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    /// <summary>ObjectName ảnh hiện trường trên bucket — giá trị lưu trong DB.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>URL public tuyệt đối của ảnh hiện trường — FE dùng thẳng làm img src.</summary>
    public string? ImageViewUrl { get; set; }
    public DateOnly? StartAt { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public DateOnly? ActualStartAt { get; set; }

    /// <summary>Số ngày theo kế hoạch (StartAt → EstimateAt), tính cả hai đầu.</summary>
    public int? PlannedDurationDays { get; set; }

    /// <summary>Số ngày thực tế (ActualStartAt → ActualAt).</summary>
    public int? ActualDurationDays { get; set; }

    /// <summary>Chi phí nhân công / thiết bị dự tính của task.</summary>
    public decimal? EstimatedLaborCost { get; set; }

    /// <summary>Chi phí nhân công / thiết bị thực chi của task.</summary>
    public decimal? ActualLaborCost { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = null!;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ConstructionTaskResponse From(SmartCoffeeBuilder.Repository.Models.ConstructionTask e) => new()
    {
        Id = e.Id,
        ConstructionItemId = e.ConstructionItemId,
        Name = e.Name,
        Description = e.Description,
        ImageUrl = e.ImageUrl,
        ImageViewUrl = MediaUrl.Resolve(e.ImageUrl),
        StartAt = e.StartAt,
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        ActualStartAt = e.ActualStartAt,
        PlannedDurationDays = ConstructionSchedule.DurationDays(e.StartAt, e.EstimateAt),
        ActualDurationDays = ConstructionSchedule.DurationDays(e.ActualStartAt, e.ActualAt),
        EstimatedLaborCost = e.EstimatedLaborCost,
        ActualLaborCost = e.ActualLaborCost,
        Reason = e.Reason,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
