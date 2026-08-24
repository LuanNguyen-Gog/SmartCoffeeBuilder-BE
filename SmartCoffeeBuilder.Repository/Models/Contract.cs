using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Contract
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }

    /// <summary>
    /// Báo giá đã được owner duyệt và dựng nên hợp đồng này (review 3). Khi có giá trị: hạng mục,
    /// giá trị hợp đồng và điều kiện thanh toán LẤY TỪ báo giá, provider không sửa được
    /// (xem ContractService.UpdateAsync). null = hợp đồng lập tay theo luồng cũ.
    /// </summary>
    public Guid? QuotationId { get; set; }
    public string Title { get; set; } = null!;
    public string? PartyInfo { get; set; }
    public string? Terms { get; set; }
    public decimal? AgreedValue { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>
    /// Ngày bắt đầu thực hiện theo hợp đồng (review 3: "Có thể bổ sung thêm các field khác bao gồm:
    /// Thời gian thực hiện"). null = hai bên chưa chốt mốc.
    /// </summary>
    public DateOnly? ExecutionStartAt { get; set; }

    /// <summary>
    /// Ngày kết thúc thực hiện theo hợp đồng. Đây là mốc CAM KẾT trong hợp đồng, khác
    /// <c>construction_items.estimate_at</c> (hạn của từng hạng mục do provider tự lập).
    /// </summary>
    public DateOnly? ExecutionEndAt { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.drafted;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account? ConfirmedByAccount { get; set; }
    public Quotation? Quotation { get; set; }

    /// <summary>Các đợt thanh toán sinh từ điều kiện thanh toán của báo giá khi hợp đồng được ký.</summary>
    public ICollection<PaymentBatch> PaymentBatches { get; set; } = new List<PaymentBatch>();
}
