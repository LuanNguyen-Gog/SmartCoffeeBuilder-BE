using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một đợt thanh toán THẬT của hợp đồng owner → provider (review 3 + KPCOS: payment_batch neo vào
/// contract, gắn được với construction_item).
///
/// Hệ thống KHÔNG giữ tiền (chốt ở review 3): owner tự chuyển khoản cho provider rồi upload minh
/// chứng (<see cref="PaymentProof"/>), provider xác nhận đã nhận → đợt chuyển 'confirmed'. Khác
/// hoàn toàn với <c>payment_transactions</c> (phí nền tảng qua payOS, tiền đi qua hệ thống).
///
/// Sinh tự động từ <see cref="QuotationPaymentTerm"/> lúc hợp đồng được ký; provider gắn thêm
/// <see cref="ConstructionItemId"/> để biết đợt tiền này ứng với hạng mục thi công nào.
/// </summary>
public class PaymentBatch
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }

    /// <summary>
    /// Hạng mục thi công tương ứng — null khi đợt tính theo mốc chung (ký hợp đồng, duyệt concept…).
    /// Khi đợt được xác nhận, hạng mục gắn ở đây sẽ được đánh dấu <c>ConstructionItem.IsPaid</c>.
    /// </summary>
    public Guid? ConstructionItemId { get; set; }

    /// <summary>Điều khoản trong báo giá đã sinh ra đợt này — giữ vết để đối chiếu, có thể null.</summary>
    public Guid? QuotationPaymentTermId { get; set; }

    /// <summary>
    /// Khoản phát sinh đã sinh ra đợt này — null với đợt sinh từ báo giá gốc.
    ///
    /// Một khoản phát sinh hai bên đã duyệt là tiền owner NỢ THẬT, nên nó phải đi ra đường thu
    /// tiền y hệt các đợt của báo giá. Thiếu cột này thì khoản duyệt xong nằm chết trong bảng
    /// change_orders: tổng công nợ có kể nó, mà không đợt nào đòi nó.
    /// </summary>
    public Guid? ChangeOrderId { get; set; }

    public int SortOrder { get; set; }
    public string Name { get; set; } = null!;
    public decimal? Percentage { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Hạn thanh toán dự kiến (nếu hai bên có chốt).</summary>
    public DateOnly? DueAt { get; set; }

    public PaymentBatchStatus Status { get; set; } = PaymentBatchStatus.pending;

    /// <summary>Mốc owner upload minh chứng gần nhất (pending/rejected → proof_submitted).</summary>
    public DateTime? ProofSubmittedAt { get; set; }

    /// <summary>Mốc provider xác nhận đã nhận tiền.</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>Account id provider đã xác nhận — lấy từ JWT.</summary>
    public Guid? ConfirmedBy { get; set; }

    /// <summary>Lý do provider từ chối minh chứng (status = rejected) — owner upload lại.</summary>
    public string? RejectReason { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Contract Contract { get; set; } = null!;
    public ConstructionItem? ConstructionItem { get; set; }
    public QuotationPaymentTerm? QuotationPaymentTerm { get; set; }
    public ChangeOrder? ChangeOrder { get; set; }
    public Account? ConfirmedByAccount { get; set; }
    public ICollection<PaymentProof> Proofs { get; set; } = new List<PaymentProof>();
}
