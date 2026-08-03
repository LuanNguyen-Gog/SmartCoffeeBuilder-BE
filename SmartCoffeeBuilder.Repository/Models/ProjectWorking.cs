using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>Trục trung tâm: engagement giữa project và provider (design/construction/both).</summary>
public class ProjectWorking
{
    public long Id { get; set; }
    public long ProjectShopOwnerId { get; set; }
    public long ServiceProviderProfileId { get; set; }
    /// <summary>nullable = thuê trực tiếp; có giá trị = qua marketplace.</summary>
    public long? ApplyId { get; set; }
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

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectShopOwner ProjectShopOwner { get; set; } = null!;
    public ServiceProviderProfile ServiceProviderProfile { get; set; } = null!;
    public Apply? Apply { get; set; }

    public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
    public ICollection<Design> Designs { get; set; } = new List<Design>();
    public ICollection<ConstructionItem> ConstructionItems { get; set; } = new List<ConstructionItem>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    public ICollection<Doc> Docs { get; set; } = new List<Doc>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
