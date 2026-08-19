using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ConstructionItem
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ParentId { get; set; } // phân cấp
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }
    // v5: bỏ IsDone — trạng thái hoàn thành chỉ đọc từ Status = completed (một nguồn sự thật).
    public ItemStatus Status { get; set; } = ItemStatus.pending;

    /// <summary>
    /// Hạng mục này đã được thanh toán chưa (review 3: "xác nhận cho từng hạng mục đã thanh toán").
    /// KHÔNG phải nguồn sự thật độc lập: PaymentBatchService bật cờ này khi đợt thanh toán gắn với
    /// hạng mục được provider xác nhận, và tắt khi đợt đó bị bác. Đọc chi tiết tiền bạc thì đi qua
    /// payment_batches — cột này chỉ để FE khỏi phải join mỗi lần vẽ danh sách hạng mục.
    /// </summary>
    public bool IsPaid { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public ConstructionItem? Parent { get; set; }
    public ICollection<ConstructionItem> Children { get; set; } = new List<ConstructionItem>();
    public Account? CreatedByAccount { get; set; }
    public ICollection<ConstructionTask> Tasks { get; set; } = new List<ConstructionTask>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();

    /// <summary>Các đợt thanh toán của hợp đồng được gắn vào chính hạng mục này.</summary>
    public ICollection<PaymentBatch> PaymentBatches { get; set; } = new List<PaymentBatch>();

    /// <summary>Checklist nghiệm thu của hạng mục thi công này (review 3).</summary>
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

    /// <summary>
    /// Vật tư khai THẲNG ở mức milestone (khi milestone không chia task). Tổng vật tư của milestone
    /// còn phải cộng thêm phần khai trong từng task con — xem <see cref="ConstructionMaterial"/>.
    /// </summary>
    public ICollection<ConstructionMaterial> Materials { get; set; } = new List<ConstructionMaterial>();
}
