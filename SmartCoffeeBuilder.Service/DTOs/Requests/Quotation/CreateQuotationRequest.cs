namespace SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;

/// <summary>
/// Provider lập báo giá. Neo vào ĐÚNG MỘT trong hai: <see cref="ApplyId"/> (báo giá kèm hồ sơ ứng
/// tuyển — owner so sánh nhiều provider rồi chọn) hoặc <see cref="ProjectWorkingId"/> (owner đã mời
/// trực tiếp). Gửi cả hai hoặc không gửi cái nào → 400.
/// </summary>
public class CreateQuotationRequest
{
    public Guid? ApplyId { get; set; }
    public Guid? ProjectWorkingId { get; set; }

    public string Title { get; set; } = null!;
    public string? Note { get; set; }

    /// <summary>Ước lượng thời gian hoàn thành (ngày).</summary>
    public int? EstimatedDurationDays { get; set; }

    /// <summary>Số lần sửa design miễn phí đi kèm báo giá (dành cho provider làm design).</summary>
    public int? FreeRevisionCount { get; set; }

    /// <summary>Phí cho MỖI vòng sửa vượt quá FreeRevisionCount (review 1.1). null = chưa công bố.</summary>
    public decimal? ExtraRevisionFee { get; set; }

    public List<QuotationItemRequest> Items { get; set; } = new();
    public List<QuotationPaymentTermRequest> PaymentTerms { get; set; } = new();
}
