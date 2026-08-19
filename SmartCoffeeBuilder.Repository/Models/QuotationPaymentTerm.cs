namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một đợt trong điều kiện thanh toán của báo giá — review 3: "30% khi ký hợp đồng, 40% khi duyệt
/// concept, 30% còn lại khi bàn giao bản vẽ kĩ thuật".
///
/// Đây mới là CAM KẾT trên giấy. Khi hợp đồng được ký (contract → confirmed), mỗi dòng ở đây sinh
/// ra một <see cref="PaymentBatch"/> — bản ghi thật để theo dõi tiền đã trả tới đâu.
/// </summary>
public class QuotationPaymentTerm
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }

    /// <summary>Thứ tự đợt (1, 2, 3…) — quyết định thứ tự sinh payment_batch.</summary>
    public int SortOrder { get; set; }

    /// <summary>Tên đợt: "Khi ký hợp đồng", "Khi duyệt concept"…</summary>
    public string Name { get; set; } = null!;

    /// <summary>Tỉ lệ % giá trị báo giá. null khi đợt được ghi bằng số tiền tuyệt đối.</summary>
    public decimal? Percentage { get; set; }

    /// <summary>Số tiền của đợt. Service tự tính từ <see cref="Percentage"/> khi client chỉ gửi %.</summary>
    public decimal Amount { get; set; }

    /// <summary>Điều kiện để đợt này tới hạn (mô tả tự do, không phải state machine).</summary>
    public string? Condition { get; set; }

    public Quotation Quotation { get; set; } = null!;
}
