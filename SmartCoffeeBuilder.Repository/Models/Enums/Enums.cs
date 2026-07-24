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
/// </summary>
public enum ProviderStatus { requested, accepted, completed, rejected, terminated }

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
#pragma warning restore CS8981
