namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;

public class ConstructionItemResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
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
        EstimateAt = e.EstimateAt,
        ActualAt = e.ActualAt,
        Status = e.Status.ToString(),
        IsPaid = e.IsPaid,
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
