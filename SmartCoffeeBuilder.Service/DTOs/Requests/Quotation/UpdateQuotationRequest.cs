namespace SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;

/// <summary>
/// Sửa báo giá khi còn 'draft'. Danh sách hạng mục / điều kiện thanh toán gửi lên là THAY TOÀN BỘ
/// (null = giữ nguyên): sửa từng dòng qua endpoint riêng sẽ đẻ ra trạng thái nửa vời giữa tổng tiền
/// và các dòng.
/// </summary>
public class UpdateQuotationRequest
{
    public string? Title { get; set; }
    public string? Note { get; set; }
    public int? EstimatedDurationDays { get; set; }
    public int? FreeRevisionCount { get; set; }

    /// <summary>Phí cho MỖI vòng sửa vượt quá FreeRevisionCount (review 1.1). null = chưa công bố.</summary>
    public decimal? ExtraRevisionFee { get; set; }
    public List<QuotationItemRequest>? Items { get; set; }
    public List<QuotationPaymentTermRequest>? PaymentTerms { get; set; }
}
