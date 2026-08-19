namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Minh chứng chuyển khoản owner upload cho một đợt thanh toán (review 3: "có thể không giữ tiền
/// nhưng phải có phần upload minh chứng giao dịch theo từng giai đoạn").
///
/// Giữ NHIỀU bản ghi thay vì một cột ảnh trên payment_batch: một đợt có thể chuyển làm nhiều lần,
/// và khi provider từ chối minh chứng thì bản cũ vẫn phải còn để đối chiếu.
/// </summary>
public class PaymentProof
{
    public Guid Id { get; set; }
    public Guid PaymentBatchId { get; set; }

    /// <summary>
    /// ObjectName ảnh/chứng từ trên bucket GCS. NULLABLE: hệ thống không giữ tiền và không nối
    /// ngân hàng, nên chủ quán được phép chỉ bấm "đã thanh toán" mà chưa đính ảnh — bản ghi này
    /// khi đó chỉ lưu mốc thời gian / số tiền / ghi chú.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>Số tiền của lần chuyển này (có thể nhỏ hơn Amount của đợt nếu trả làm nhiều lần).</summary>
    public decimal? Amount { get; set; }

    /// <summary>Thời điểm chuyển khoản theo chứng từ — khác CreatedAt (lúc upload lên hệ thống).</summary>
    public DateTime? TransferredAt { get; set; }

    public string? Note { get; set; }

    /// <summary>Account id owner đã upload — lấy từ JWT.</summary>
    public Guid? UploadedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public PaymentBatch PaymentBatch { get; set; } = null!;
    public Account? UploadedByAccount { get; set; }
}
