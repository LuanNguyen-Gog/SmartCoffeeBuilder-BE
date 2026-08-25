using SmartCoffeeBuilder.Service.Utils;
namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;

public class ConstructionItemResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }

    /// <summary>
    /// Thứ tự nhà thầu đã sắp trong nhóm anh em (nhỏ hơn đứng trước). FE cần trả ra để dựng lại
    /// đúng thứ tự sau khi kéo thả mà không phải đoán từ ngày tháng.
    /// </summary>
    public int SortOrder { get; set; }

    public DateOnly? StartAt { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    public DateOnly? ActualStartAt { get; set; }

    /// <summary>Số ngày theo KẾ HOẠCH (StartAt → EstimateAt), tính cả hai đầu. null khi thiếu mốc.</summary>
    public int? PlannedDurationDays { get; set; }

    /// <summary>Số ngày THỰC TẾ (ActualStartAt → ActualAt). null khi hạng mục chưa xong.</summary>
    public int? ActualDurationDays { get; set; }

    /// <summary>Chi phí nhân công / thiết bị dự tính. Chi phí vật tư xem /api/materials/cost.</summary>
    public decimal? EstimatedLaborCost { get; set; }

    /// <summary>Chi phí nhân công / thiết bị thực chi.</summary>
    public decimal? ActualLaborCost { get; set; }
    public string Status { get; set; } = null!;

    /// <summary>
    /// Hạng mục đã được thanh toán chưa — bật/tắt bởi <c>PaymentBatchService</c> khi đợt thanh toán
    /// gắn vào hạng mục được provider xác nhận / bị bác. Trả kèm ở đây để FE vẽ được danh sách hạng
    /// mục kèm tình trạng tiền mà không phải join sang payment_batches.
    /// </summary>
    public bool IsPaid { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ConstructionItemResponse From(SmartCoffeeBuilder.Repository.Models.ConstructionItem e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        ParentId = e.ParentId,
        Name = e.Name,
        Description = e.Description,
        Category = e.Category,
        SortOrder = e.SortOrder,
        StartAt = e.StartAt,
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        ActualStartAt = e.ActualStartAt,
        PlannedDurationDays = ConstructionSchedule.DurationDays(e.StartAt, e.EstimateAt),
        ActualDurationDays = ConstructionSchedule.DurationDays(e.ActualStartAt, e.ActualAt),
        EstimatedLaborCost = e.EstimatedLaborCost,
        ActualLaborCost = e.ActualLaborCost,
        Status = e.Status.ToString(),
        IsPaid = e.IsPaid,
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
