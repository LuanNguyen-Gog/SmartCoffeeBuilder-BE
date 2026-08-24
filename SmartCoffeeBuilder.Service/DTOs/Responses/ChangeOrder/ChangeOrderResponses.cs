using Entities = SmartCoffeeBuilder.Repository.Models;
using Enums = SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ChangeOrder;

/// <summary>Một khoản phát sinh chi phí ngoài báo giá đã chốt.</summary>
public class ChangeOrderResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? DesignId { get; set; }
    public Guid? ConstructionItemId { get; set; }
    public string Kind { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public decimal Amount { get; set; }

    /// <summary>Vòng sửa đã kích hoạt khoản này — chỉ có với kind = extra_revision.</summary>
    public int? RevisionNo { get; set; }

    public string Status { get; set; } = null!;

    /// <summary>owner | provider — bên đã lập khoản này.</summary>
    public string RequestedByParty { get; set; } = null!;

    public Guid? CreatedBy { get; set; }
    public Guid? RespondedBy { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Đợt thanh toán đã sinh ra khi khoản này được duyệt. null = chưa ra đợt thu: khoản còn treo,
    /// bị từ chối, bằng 0 đồng, hoặc hợp tác chưa ký hợp đồng để gắn đợt vào.
    /// </summary>
    public Guid? PaymentBatchId { get; set; }

    /// <summary>pending | proof_submitted | confirmed | rejected — trạng thái đợt thu ở trên.</summary>
    public string? PaymentBatchStatus { get; set; }

    /// <summary>
    /// Khoản phí sửa hệ thống dựng sẵn mà nhà cung cấp CHƯA điền đơn giá (báo giá không công bố
    /// <c>extra_revision_fee</c>). FE dùng cờ này để bảo provider vào điền số, thay vì để owner nhìn
    /// một khoản 0 đồng không hiểu chờ ai.
    /// </summary>
    public bool NeedsPricing { get; set; }

    public static ChangeOrderResponse From(Entities.ChangeOrder e) => From(e, null);

    public static ChangeOrderResponse From(Entities.ChangeOrder e, Entities.PaymentBatch? batch) => new()
    {
        PaymentBatchId = batch?.Id,
        PaymentBatchStatus = batch?.Status.ToString(),
        NeedsPricing = e.Kind == Enums.ChangeOrderKind.extra_revision
                       && e.Status == Enums.ChangeOrderStatus.pending
                       && e.Amount <= 0m,
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        DesignId = e.DesignId,
        ConstructionItemId = e.ConstructionItemId,
        Kind = e.Kind.ToString(),
        Title = e.Title,
        Reason = e.Reason,
        Amount = e.Amount,
        RevisionNo = e.RevisionNo,
        Status = e.Status.ToString(),
        RequestedByParty = e.RequestedByParty.ToString(),
        CreatedBy = e.CreatedBy,
        RespondedBy = e.RespondedBy,
        RespondedAt = e.RespondedAt,
        RejectReason = e.RejectReason,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

/// <summary>
/// Tổng công nợ của một hợp tác: giá trị hợp đồng CỘNG các khoản phát sinh đã được hai bên duyệt.
/// Đây là con số duy nhất trả lời được "rốt cuộc dự án này tốn bao nhiêu" — giá trị hợp đồng một
/// mình không kể phần phí sửa và phần đổi phạm vi phát sinh về sau.
/// </summary>
public class ChangeOrderSummaryResponse
{
    public Guid ProjectWorkingId { get; set; }

    /// <summary>Giá trị hợp đồng đã ký (contracts.agreed_value). null khi chưa ký hợp đồng.</summary>
    public decimal? ContractValue { get; set; }

    /// <summary>Tổng các khoản phát sinh đã được duyệt.</summary>
    public decimal AcceptedAmount { get; set; }

    /// <summary>Tổng các khoản còn treo chờ bên kia trả lời — chưa tính vào công nợ.</summary>
    public decimal PendingAmount { get; set; }

    /// <summary>= ContractValue + AcceptedAmount. null khi chưa có hợp đồng để cộng vào.</summary>
    public decimal? TotalCommitted { get; set; }

    public int AcceptedCount { get; set; }
    public int PendingCount { get; set; }
    public int RejectedCount { get; set; }

    /// <summary>Riêng phần phí sửa thiết kế đã duyệt — tách ra vì review 1.1 hỏi đích danh.</summary>
    public decimal AcceptedRevisionFee { get; set; }

    /// <summary>
    /// Phần công nợ đã duyệt ĐÃ ra được đợt thu thật trong <c>payment_batches</c>.
    /// "Đã duyệt" mới là hai bên đồng ý con số; có đợt thu mới là có đường đòi.
    /// </summary>
    public decimal BilledAmount { get; set; }

    /// <summary>Phần đã ra đợt thu VÀ provider đã xác nhận nhận được tiền.</summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// = AcceptedAmount − BilledAmount. Dương nghĩa là còn tiền hai bên đã đồng ý nhưng chưa vào
    /// được đường thu — gần như luôn vì hợp tác chưa ký hợp đồng để gắn đợt vào.
    /// </summary>
    public decimal UnbilledAmount { get; set; }
}

/// <summary>
/// Tình trạng hạn mức sửa của một bản thiết kế: đã dùng mấy vòng, còn mấy vòng miễn phí,
/// vòng tiếp theo tốn bao nhiêu.
/// </summary>
public class RevisionQuotaResponse
{
    public Guid DesignId { get; set; }

    /// <summary>Hợp tác mà hạn mức này thuộc về — hạn mức tiêu chung ở phạm vi này.</summary>
    public Guid ProjectWorkingId { get; set; }

    /// <summary>Báo giá đang chi phối. null = chưa có báo giá chốt ⇒ không giới hạn.</summary>
    public Guid? QuotationId { get; set; }

    /// <summary>Số vòng miễn phí cam kết. null = không cam kết ⇒ không giới hạn.</summary>
    public int? FreeRevisionCount { get; set; }

    /// <summary>Số vòng owner đã yêu cầu sửa TRÊN CHÍNH bản thiết kế này.</summary>
    public int UsedRevisionCount { get; set; }

    /// <summary>
    /// Số vòng đã dùng trên TOÀN hợp tác (cộng mọi bản thiết kế). Đây mới là con số đem so với
    /// hạn mức: cam kết "N lần sửa miễn phí" nằm trên báo giá, mà báo giá phủ cả hợp tác.
    /// </summary>
    public int EngagementUsedRevisionCount { get; set; }

    /// <summary>Số vòng miễn phí còn lại của cả hợp tác. null khi không giới hạn.</summary>
    public int? RemainingFreeRevisions { get; set; }

    /// <summary>Vòng sửa kế tiếp có phát sinh phí không.</summary>
    public bool NextRevisionCharged { get; set; }

    /// <summary>Đơn giá cho vòng vượt hạn mức. null = provider chưa công bố.</summary>
    public decimal? ExtraRevisionFee { get; set; }
}
