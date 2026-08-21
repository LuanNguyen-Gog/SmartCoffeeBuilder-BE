using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một khoản phát sinh chi phí NGOÀI báo giá đã chốt (review 1.1: "quy định số lần sửa và phí sửa").
///
/// Vì sao phải có bảng riêng thay vì một cột tiền trên quotation: báo giá đã được duyệt thì bị KHOÁ
/// (<c>quotations.locked_at</c>) — cộng thêm tiền vào đó là sửa đè một con số hai bên đã ký. Phát
/// sinh là một cam kết MỚI, có bên đề nghị, có bên đồng ý, có mốc thời gian riêng, nên nó là bản
/// ghi riêng và cộng vào tổng công nợ chứ không đụng vào báo giá gốc.
///
/// Nguồn phổ biến nhất: <see cref="ChangeOrderKind.extra_revision"/> — owner yêu cầu sửa thiết kế
/// vượt quá <c>quotations.free_revision_count</c>. <c>DesignService.RequestRevisionAsync</c> tạo
/// bản ghi này ngay tại vòng vượt hạn mức, với đơn giá lấy từ <c>quotations.extra_revision_fee</c>.
/// </summary>
public class ChangeOrder
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>project_providers.id</c>. Phát sinh luôn thuộc về một hợp tác cụ thể.</summary>
    public Guid ProjectWorkingId { get; set; }

    /// <summary>
    /// Bản thiết kế đã kéo theo phát sinh này — chỉ có giá trị với
    /// <see cref="ChangeOrderKind.extra_revision"/>. FK SET NULL: xoá design không xoá công nợ.
    /// </summary>
    public Guid? DesignId { get; set; }

    /// <summary>Hạng mục thi công liên quan (scope/material change). FK SET NULL.</summary>
    public Guid? ConstructionItemId { get; set; }

    public ChangeOrderKind Kind { get; set; }

    public string Title { get; set; } = null!;

    /// <summary>Vì sao phát sinh — bắt buộc có, đây là thứ bên kia đọc để quyết định đồng ý.</summary>
    public string Reason { get; set; } = null!;

    /// <summary>Số tiền phát sinh. Không âm; 0 hợp lệ (ghi nhận thay đổi mà không tính tiền).</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Vòng sửa thứ mấy đã kích hoạt khoản này — chỉ điền cho <c>extra_revision</c>, để đối chiếu
    /// với <c>designs.revision_count</c> khi có tranh cãi "đã sửa mấy lần rồi".
    /// </summary>
    public int? RevisionNo { get; set; }

    public ChangeOrderStatus Status { get; set; } = ChangeOrderStatus.pending;

    /// <summary>Bên đã lập khoản phát sinh (owner yêu cầu sửa, hay provider báo đổi phạm vi).</summary>
    public EngagementParty RequestedByParty { get; set; }

    /// <summary>Account id người lập — lấy từ JWT.</summary>
    public Guid? CreatedBy { get; set; }

    /// <summary>Account id người phản hồi (đồng ý / từ chối) — lấy từ JWT.</summary>
    public Guid? RespondedBy { get; set; }

    public DateTime? RespondedAt { get; set; }

    /// <summary>Lý do bên kia từ chối khoản phát sinh này.</summary>
    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Đợt thanh toán sinh ra khi khoản này được duyệt — rỗng khi khoản còn treo, bị từ chối,
    /// bằng 0 đồng, hoặc engagement chưa có hợp đồng đã ký để gắn đợt vào.
    /// </summary>
    public ICollection<PaymentBatch> PaymentBatches { get; set; } = new List<PaymentBatch>();

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Design? Design { get; set; }
    public ConstructionItem? ConstructionItem { get; set; }
    public Account? CreatedByAccount { get; set; }
    public Account? RespondedByAccount { get; set; }
}
