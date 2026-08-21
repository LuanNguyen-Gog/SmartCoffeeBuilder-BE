using SmartCoffeeBuilder.Service.Utils;
using Entity = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.PaymentBatch;

/// <summary>
/// Một đợt thanh toán của hợp đồng, kèm lịch sử minh chứng owner đã upload.
/// </summary>
public class PaymentBatchResponse
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid? ConstructionItemId { get; set; }
    public string? ConstructionItemName { get; set; }

    /// <summary>
    /// Khoản phát sinh đã sinh ra đợt này — null với đợt chia từ báo giá gốc. FE dùng để nói rõ
    /// "đợt này là phát sinh phát thêm", chứ không lẫn vào các đợt của hợp đồng ban đầu.
    /// </summary>
    public Guid? ChangeOrderId { get; set; }

    public int SortOrder { get; set; }
    public string Name { get; set; } = null!;
    public decimal? Percentage { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? DueAt { get; set; }

    /// <summary>pending | proof_submitted | confirmed | rejected</summary>
    public string Status { get; set; } = null!;

    public DateTime? ProofSubmittedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public string? RejectReason { get; set; }
    public string? Note { get; set; }

    /// <summary>Tổng số tiền owner đã khai trên các minh chứng đã upload.</summary>
    public decimal PaidAmount { get; set; }

    public List<PaymentProofResponse> Proofs { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static PaymentBatchResponse From(Entity.PaymentBatch e) => new()
    {
        Id = e.Id,
        ContractId = e.ContractId,
        ConstructionItemId = e.ConstructionItemId,
        ConstructionItemName = e.ConstructionItem?.Name,
        ChangeOrderId = e.ChangeOrderId,
        SortOrder = e.SortOrder,
        Name = e.Name,
        Percentage = e.Percentage,
        Amount = e.Amount,
        DueAt = e.DueAt,
        Status = e.Status.ToString(),
        ProofSubmittedAt = e.ProofSubmittedAt,
        ConfirmedAt = e.ConfirmedAt,
        ConfirmedBy = e.ConfirmedBy,
        RejectReason = e.RejectReason,
        Note = e.Note,
        // Minh chứng không khai số tiền thì coi như trả đúng giá trị đợt.
        PaidAmount = e.Proofs.Sum(p => p.Amount ?? 0m),
        Proofs = e.Proofs.OrderByDescending(p => p.CreatedAt).Select(PaymentProofResponse.From).ToList(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

public class PaymentProofResponse
{
    public Guid Id { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageViewUrl { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? TransferredAt { get; set; }
    public string? Note { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public static PaymentProofResponse From(Entity.PaymentProof e) => new()
    {
        Id = e.Id,
        ImageUrl = e.ImageUrl,
        ImageViewUrl = MediaUrl.Resolve(e.ImageUrl),
        Amount = e.Amount,
        TransferredAt = e.TransferredAt,
        Note = e.Note,
        UploadedBy = e.UploadedBy,
        CreatedAt = e.CreatedAt
    };
}
