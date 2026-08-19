using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Trục trung tâm: engagement giữa project và provider (design/construction/both).</summary>
public class ProjectWorking
{
    public Guid Id { get; set; }
    public Guid ProjectShopOwnerId { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    /// <summary>nullable = thuê trực tiếp; có giá trị = qua marketplace.</summary>
    public Guid? ApplyId { get; set; }
    public ServiceKind ContractType { get; set; }
    public ProviderStatus Status { get; set; } = ProviderStatus.requested;
    public string? RequestMessage { get; set; }
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Mốc provider bấm "đã xong việc, mời owner nghiệm thu". null = chưa xin nghiệm thu.
    /// KHÔNG phải một trạng thái mới — ProviderStatus vẫn giữ đúng 5 giá trị v5;
    /// "chờ nghiệm thu" là DERIVED: status = accepted và cột này khác null.
    /// </summary>
    public DateTime? CompletionRequestedAt { get; set; }

    /// <summary>Ghi chú bàn giao provider gửi kèm khi xin nghiệm thu.</summary>
    public string? CompletionRequestNote { get; set; }

    /// <summary>
    /// Mốc một bên đề nghị huỷ ngang. null = không có đề nghị nào đang treo.
    /// Huỷ ngang cần ĐỒNG THUẬN HAI BÊN: một bên đề nghị (đặt cột này), bên kia đồng ý thì
    /// status mới chuyển sang 'terminated'. KHÔNG phải trạng thái mới — ProviderStatus vẫn giữ
    /// đúng 5 giá trị v5; "chờ duyệt huỷ ngang" là DERIVED: accepted + cột này khác null.
    /// </summary>
    public DateTime? TerminationRequestedAt { get; set; }

    /// <summary>Bên đã gửi đề nghị huỷ ngang (owner hay provider). Giữ lại sau khi huỷ để làm vết.</summary>
    public EngagementParty? TerminationRequestedBy { get; set; }

    /// <summary>Lý do huỷ ngang bên đề nghị gửi kèm.</summary>
    public string? TerminationRequestNote { get; set; }

    /// <summary>Mốc hai bên chốt huỷ ngang (bên còn lại bấm đồng ý). null = chưa huỷ.</summary>
    public DateTime? TerminatedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectShopOwner ProjectShopOwner { get; set; } = null!;
    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
    public Apply? Apply { get; set; }

    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    public ICollection<Design> Designs { get; set; } = new List<Design>();
    // KHÔNG có DesignVersions ở đây: design_version treo vào Design (design_id), không có FK về
    // project_provider. Navigation cũ là rác — nó làm model snapshot không nạp được (EF 10).
    public ICollection<ConstructionItem> ConstructionItems { get; set; } = new List<ConstructionItem>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Doc> Docs { get; set; } = new List<Doc>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    /// <summary>Báo giá của đường MỜI TRỰC TIẾP (không qua hồ sơ ứng tuyển).</summary>
    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();

    /// <summary>Bảng giá vật tư đã công bố cho engagement này (review 3).</summary>
    public ICollection<Material> Materials { get; set; } = new List<Material>();
}
