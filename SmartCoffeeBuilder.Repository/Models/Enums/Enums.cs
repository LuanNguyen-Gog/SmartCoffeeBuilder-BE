// Enum members are named to match the exact text value stored in the DB.
// Combined with HaveConversion<string>(), they persist as e.g. "in_progress".
#pragma warning disable CS8981 // lowercase type/member names are intentional (DB text values)
namespace SmartCoffeeBuilder.Repository.Models.Enums;

public enum AccountRole { owner, provider, admin }

public enum AccountStatus { active, inactive, banned, pending }

public enum ProviderType { individual, company }

public enum Capability { designer, constructor, both }

public enum ProjectStatus { briefed, in_progress, completed, cancelled }

/// <summary>Dùng chung cho project_post.service_kind và project_provider.contract_type.</summary>
public enum ServiceKind { design, construction, both }

public enum PostStatus { open, closed, cancelled }

public enum ApplicationStatus { pending, accepted, rejected }

/// <summary>
/// Trạng thái QUAN HỆ HỢP TÁC (mời → nhận → xong/từ chối/huỷ ngang), KHÔNG phải state machine tiến độ.
/// - "Đang thực hiện" là DERIVED: accepted + tồn tại contract confirmed.
/// - Pha design/construction là DERIVED: contract_type + trạng thái design / construction_item con.
/// Transitions: requested → accepted | rejected; accepted → completed | terminated.
/// - accepted → terminated cần ĐỒNG THUẬN HAI BÊN: một bên đề nghị (termination_requested_at/_by),
///   bên còn lại đồng ý thì mới chuyển. Không bên nào tự huỷ thẳng được (admin can thiệp là ngoại lệ).
/// </summary>
public enum ProviderStatus { requested, accepted, completed, rejected, terminated }

/// <summary>
/// Bên tham gia một engagement — dùng để ghi "ai là người đề nghị huỷ ngang"
/// (<c>project_provider.termination_requested_by</c>). KHÔNG phải role tài khoản:
/// cùng một account có thể là owner ở engagement này và provider ở engagement khác.
/// </summary>
public enum EngagementParty { owner, provider }

public enum DesignStatus { in_progress, submitted, revision, approved }

public enum DesignType { concept, layout_2d, render_3d, technical_drawing }

/// <summary>Dùng chung cho construction_item VÀ construction_task.</summary>
public enum ItemStatus { pending, in_progress, completed }

public enum IssueStatus { open, in_progress, resolved, closed }

public enum ContractStatus { drafted, pending_otp, confirmed, cancelled }

/// <summary>
/// Vòng đời subscription (phí nền tảng):
/// pending → active (webhook payOS báo đã thanh toán) | cancelled (huỷ/hết hạn link);
/// active → expired (quá EndDate, job nền quét).
/// </summary>
public enum SubscriptionStatus { pending, active, expired, cancelled }

/// <summary>Trạng thái giao dịch payOS: pending → paid | cancelled | failed.</summary>
public enum PaymentTransactionStatus { pending, paid, cancelled, failed }

/// <summary>
/// Mục đích giao dịch payOS: mua gói phí nền tảng (subscription)
/// hay trả phí đẩy bài đăng tuyển provider lên đầu danh sách (post_boost).
/// </summary>
public enum PaymentPurpose { subscription, post_boost }

/// <summary>
/// Nền tảng khởi tạo giao dịch — quyết định cặp returnUrl/cancelUrl gửi cho payOS.
/// Web và mobile nằm trên hai domain khác nhau nên link đã tạo cho bên này KHÔNG dùng lại được cho bên kia.
/// </summary>
public enum PaymentPlatform { web, mobile }

/// <summary>
/// Loại entity mà một <c>Comment</c> neo vào. Dùng FK mềm (target_type + target_id) để một bảng
/// <c>comments</c> phục vụ thread cho nhiều entity (ConstructionItem, Design) — không phải 2 bảng rồi UNION.
/// Mở rộng thêm giá trị khi cần comment cho entity mới.
/// </summary>
public enum CommentTargetType { construction_item, design }

/// <summary>
/// Loại snapshot của <c>DesignVersion</c> — mỗi mốc quan trọng sinh một bản BẤT BIẾN:
/// <c>submitted</c> provider nộp bản để duyệt; <c>approved</c> owner duyệt (có thể nhiều bản nếu
/// design được approve lại sau revision); <c>revision</c> owner trả về kèm lý do.
///
/// <c>revision</c> là chỗ DUY NHẤT giữ lý do của TỪNG vòng sửa: <c>designs.reason</c> chỉ có một ô
/// và bị ghi đè ở vòng kế tiếp, nên muốn xem lại lý do cũ phải đọc snapshot.
/// </summary>
public enum DesignVersionSnapshotKind { submitted, approved, revision }

/// <summary>
/// Vòng đời báo giá (review 3): draft → sent → accepted | rejected | revision_requested.
/// - <c>revision_requested</c>: owner yêu cầu bản khác kèm lý do; provider phát hành bản mới
///   (version +1) chứ KHÔNG sửa đè bản đã gửi.
/// - <c>accepted</c>: khoá bản báo giá (locked_at), là bản duy nhất được dựng hợp đồng.
/// - <c>superseded</c>: bản cũ bị thay khi một bản khác cùng chỗ neo được duyệt.
/// </summary>
public enum QuotationStatus { draft, sent, revision_requested, accepted, rejected, superseded }

/// <summary>
/// Vòng đời một đợt thanh toán owner → provider. Hệ thống KHÔNG giữ tiền, nên trạng thái phản ánh
/// tiến trình ĐỐI CHIẾU chứ không phải trạng thái giao dịch ngân hàng:
/// pending → proof_submitted (owner upload minh chứng) → confirmed (provider xác nhận đã nhận)
/// | rejected (provider bác minh chứng → owner upload lại, quay về proof_submitted).
/// </summary>
public enum PaymentBatchStatus { pending, proof_submitted, confirmed, rejected }

/// <summary>
/// Kết quả chấm một mục nghiệm thu (review 3): pending (chưa chấm) → passed | failed.
/// Owner chấm lại được bao nhiêu lần cũng được — nghiệm thu là đối thoại, không phải state machine
/// một chiều: 'failed' kèm ghi chú "cần sửa gì", provider sửa xong thì owner chấm lại thành 'passed'.
/// </summary>
public enum ChecklistStatus { pending, passed, failed }

/// <summary>
/// Tiêu chí chấm điểm provider — DANH SÁCH CỐ ĐỊNH, không phải chuỗi tự do.
///
/// Trước đây <c>review_scores.dimension</c> là text: mỗi người gõ một kiểu ("Tiến độ",
/// "Tien do", "Tiến độ thi công") nên phần tổng hợp điểm trung bình theo tiêu chí tách
/// thành nhiều dòng rời rạc và không so sánh được giữa các provider. Đóng khung thành enum
/// để mọi provider được chấm trên cùng một bộ thước đo.
/// </summary>
public enum ReviewDimension
{
    /// <summary>Tiến độ — bám sát mốc thời gian đã cam kết.</summary>
    progress,
    /// <summary>Chất lượng — thành phẩm thiết kế / thi công.</summary>
    quality,
    /// <summary>Giao tiếp — phản hồi, cập nhật tình hình.</summary>
    communication,
    /// <summary>Chi phí — bám sát báo giá, không phát sinh tuỳ tiện.</summary>
    cost,
    /// <summary>Thái độ làm việc — chuyên nghiệp, giữ cam kết.</summary>
    professionalism
}

/// <summary>
/// Đơn vị tính của vật tư (review 3: "định nghĩa bằng tiền/đơn vị"). Cố định thành enum để
/// đơn giá còn cộng trừ được — đơn vị tự do thì "m2" và "M²" thành hai thứ khác nhau.
/// </summary>
public enum MaterialUnit
{
    /// <summary>Mét dài (md).</summary>
    md,
    /// <summary>Mét vuông (m²).</summary>
    m2,
    /// <summary>Mét khối (m³).</summary>
    m3,
    /// <summary>Kilôgam.</summary>
    kg,
    /// <summary>Lít.</summary>
    litre,
    /// <summary>Cái / chiếc — đếm từng đơn vị (bóng đèn, ổ cắm…).</summary>
    item,
    /// <summary>Bộ — cụm nhiều món bán kèm nhau.</summary>
    set,
    /// <summary>Công (ngày công nhân).</summary>
    manday
}

/// <summary>
/// Hướng của mặt bằng hoặc của một ô cửa (review 1.1: "gắn thông số thực tế như … hướng").
/// Tám hướng cố định thay vì text tự do: hướng là thứ AI và người thiết kế phải suy luận trên đó
/// (nắng chiều, gió, chỗ đặt biển hiệu). "Đông Nam" và "dong nam" mà thành hai giá trị khác nhau
/// thì không lọc, không thống kê và không đưa vào prompt được.
/// </summary>
public enum Orientation { north, northeast, east, southeast, south, southwest, west, northwest }

/// <summary>
/// Loại ô mở trên mặt bằng (review 1.1: "cửa, ban công"). Ô mở quyết định lối vào, ánh sáng tự
/// nhiên và chỗ đặt mặt tiền — là ràng buộc thiết kế thật, không phải ghi chú.
/// </summary>
public enum SiteOpeningType
{
    /// <summary>Cửa chính — lối vào khách.</summary>
    main_door,
    /// <summary>Cửa phụ / lối thoát hiểm.</summary>
    secondary_door,
    /// <summary>Cửa phục vụ: nhập hàng, đổ rác, bếp.</summary>
    service_door,
    /// <summary>Cửa sổ.</summary>
    window,
    /// <summary>Ban công.</summary>
    balcony,
    /// <summary>Sân thượng.</summary>
    terrace,
    /// <summary>Giếng trời.</summary>
    skylight
}

/// <summary>
/// Loại phát sinh chi phí NGOÀI báo giá đã chốt (review 1.1: "quy định số lần sửa và phí sửa").
/// </summary>
public enum ChangeOrderKind
{
    /// <summary>Vòng sửa thiết kế vượt quá số lần miễn phí cam kết trong báo giá.</summary>
    extra_revision,
    /// <summary>Owner đổi phạm vi công việc so với báo giá.</summary>
    scope_change,
    /// <summary>Đổi vật tư so với bảng giá đã công bố.</summary>
    material_change,
    /// <summary>Phát sinh khác.</summary>
    other
}

/// <summary>
/// Vòng đời một phát sinh chi phí: <c>pending</c> (đã lập, chờ bên kia đồng ý) →
/// <c>accepted</c> | <c>rejected</c>. Bản <c>accepted</c> bị KHOÁ, muốn đổi thì lập bản mới —
/// cùng nguyên tắc với <see cref="QuotationStatus"/>: số tiền hai bên đã đồng ý không sửa đè.
/// </summary>
public enum ChangeOrderStatus { pending, accepted, rejected }

/// <summary>
/// Kênh mạng xã hội / website của nhà cung cấp (review 1.1: "thương hiệu"). Đóng khung thành enum
/// thay vì để mỗi dòng một chuỗi tự do: FE cần biết vẽ icon nào, và "Facebook" / "facebook" / "FB"
/// mà thành ba nền tảng khác nhau thì không nhóm được.
/// </summary>
public enum SocialPlatform { facebook, instagram, tiktok, youtube, linkedin, zalo, website, other }

/// <summary>
/// Loại hồ sơ năng lực đính kèm (review 1.1: "năng lực"): giấy phép hành nghề, chứng chỉ chuyên
/// môn, giải thưởng, tư cách thành viên hiệp hội.
/// </summary>
public enum CertificateKind
{
    /// <summary>Giấy phép hành nghề / đăng ký kinh doanh.</summary>
    license,
    /// <summary>Chứng chỉ chuyên môn.</summary>
    certificate,
    /// <summary>Giải thưởng.</summary>
    award,
    /// <summary>Tư cách thành viên hiệp hội nghề.</summary>
    membership,
    /// <summary>Loại khác.</summary>
    other
}
#pragma warning restore CS8981
