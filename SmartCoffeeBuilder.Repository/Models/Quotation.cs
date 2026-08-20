using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Báo giá provider gửi owner TRƯỚC khi ký hợp đồng (chốt ở review 3 ngày 17/08/2026).
///
/// Neo vào ĐÚNG MỘT trong hai đường vào hệ thống — enforce bằng CHECK <c>ck_quotations_anchor</c>:
/// <list type="bullet">
/// <item><see cref="ApplyId"/> — marketplace: báo giá đi kèm hồ sơ ứng tuyển. Đây là điểm chính của
/// review 3: trước đây owner chỉ thấy đúng một dòng chữ <c>Apply.Proposal</c> nên không so sánh
/// được các provider cùng ứng tuyển.</item>
/// <item><see cref="ProjectWorkingId"/> — owner mời trực tiếp: không có hồ sơ ứng tuyển để neo,
/// báo giá treo thẳng vào engagement (vẫn phải chốt trước khi lập hợp đồng).</item>
/// </list>
///
/// Một chỗ neo có NHIỀU bản báo giá (<see cref="Version"/> tăng dần). Bản được owner duyệt bị
/// KHOÁ (<see cref="LockedAt"/>) — muốn đổi thì phát hành bản mới, không sửa đè lịch sử.
/// </summary>
public class Quotation
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>project_applications.id</c>. null khi báo giá thuộc đường mời trực tiếp.</summary>
    public Guid? ApplyId { get; set; }

    /// <summary>FK -> <c>project_providers.id</c>. null khi báo giá đi kèm hồ sơ ứng tuyển.</summary>
    public Guid? ProjectWorkingId { get; set; }

    /// <summary>Số hiệu bản trong cùng một chỗ neo, bắt đầu từ 1. Service tự cấp, client không gửi.</summary>
    public int Version { get; set; } = 1;

    public string Title { get; set; } = null!;

    /// <summary>Ghi chú chung / điều khoản kèm báo giá.</summary>
    public string? Note { get; set; }

    /// <summary>
    /// Tổng thành tiền = SUM(<see cref="QuotationItem.Amount"/>). Service tính lại mỗi lần đổi
    /// hạng mục — KHÔNG nhận từ client, để con số trên hợp đồng không lệch với bảng hạng mục.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Ước lượng thời gian hoàn thành, tính bằng ngày (review 3: "VD: 90 ngày").</summary>
    public int? EstimatedDurationDays { get; set; }

    /// <summary>
    /// Số lần owner được yêu cầu sửa design MIỄN PHÍ (review 3, mục Designer). Vượt quá thì
    /// provider có quyền lập yêu cầu phát sinh chi phí. null = không cam kết con số nào.
    /// </summary>
    public int? FreeRevisionCount { get; set; }

    /// <summary>
    /// Phí cho MỖI vòng sửa vượt quá <see cref="FreeRevisionCount"/> (review 1.1: "quy định số lần
    /// sửa và phí sửa"). null = provider không công bố phí, vượt hạn mức thì hai bên tự thoả thuận
    /// bằng một <see cref="ChangeOrder"/> lập tay.
    /// </summary>
    public decimal? ExtraRevisionFee { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.draft;

    /// <summary>Lý do owner yêu cầu bản khác (status = revision_requested).</summary>
    public string? RevisionReason { get; set; }

    /// <summary>Lý do owner từ chối hẳn bản báo giá này (status = rejected).</summary>
    public string? RejectReason { get; set; }

    /// <summary>Mốc provider gửi bản này cho owner (draft → sent).</summary>
    public DateTime? SentAt { get; set; }

    /// <summary>Mốc owner phản hồi (duyệt / từ chối / yêu cầu sửa).</summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>Account id của owner đã phản hồi — lấy từ JWT, không nhận từ body.</summary>
    public Guid? RespondedBy { get; set; }

    /// <summary>
    /// Mốc khoá bản báo giá: đã được owner duyệt thì không sửa được nữa (review 3).
    /// Muốn đổi phải phát hành bản mới, bản cũ chuyển 'superseded'.
    /// </summary>
    public DateTime? LockedAt { get; set; }

    /// <summary>Account id provider đã lập bản báo giá.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Apply? Apply { get; set; }
    public ProjectWorking? ProjectWorking { get; set; }
    public Account? CreatedByAccount { get; set; }
    public Account? RespondedByAccount { get; set; }

    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
    public ICollection<QuotationPaymentTerm> PaymentTerms { get; set; } = new List<QuotationPaymentTerm>();
    public ICollection<QuotationAttachment> Attachments { get; set; } = new List<QuotationAttachment>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}
