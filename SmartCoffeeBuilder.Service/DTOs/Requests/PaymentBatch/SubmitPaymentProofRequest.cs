namespace SmartCoffeeBuilder.Service.DTOs.Requests.PaymentBatch;

/// <summary>
/// Owner báo đã chuyển tiền cho một đợt: ảnh/chứng từ đã upload qua api/files (gửi ObjectName hoặc
/// URL public của bucket). Hệ thống KHÔNG giữ tiền nên đây là bằng chứng duy nhất để provider đối chiếu.
/// </summary>
public class SubmitPaymentProofRequest
{
    /// <summary>ObjectName ảnh chứng từ — để trống nếu chỉ muốn đánh dấu đã thanh toán.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Số tiền của lần chuyển này — để trống nếu chuyển đúng bằng giá trị đợt.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Thời điểm chuyển khoản theo chứng từ (khác thời điểm upload).</summary>
    public DateTime? TransferredAt { get; set; }

    public string? Note { get; set; }
}
