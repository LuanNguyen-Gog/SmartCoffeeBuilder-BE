namespace SmartCoffeeBuilder.Service.DTOs.Requests.PaymentBatch;

/// <summary>Provider bác minh chứng (sai số tiền, ảnh không đọc được…) — owner upload lại.</summary>
public class RejectPaymentBatchRequest
{
    public string? Reason { get; set; }
}
