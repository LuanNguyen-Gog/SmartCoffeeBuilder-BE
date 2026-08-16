using SmartCoffeeBuilder.Service.Utils;
using ContractModel = SmartCoffeeBuilder.Repository.Models.Contract;
using ContractStatusEnum = SmartCoffeeBuilder.Repository.Models.Enums.ContractStatus;
using ProjectWorkingModel = SmartCoffeeBuilder.Repository.Models.ProjectWorking;
using ProviderStatusEnum = SmartCoffeeBuilder.Repository.Models.Enums.ProviderStatus;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

/// <summary>
/// Contract hiện hành của engagement, đính kèm trong <see cref="ProjectWorkingResponse"/>.
/// Chỉ chứa giá trị vô hướng — KHÔNG trỏ ngược về ProjectWorking nên không có vòng lặp
/// khi serialize (ContractResponse đầy đủ vẫn lấy qua api/contracts).
/// </summary>
public class EngagementContractSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public decimal? AgreedValue { get; set; }
    /// <summary>URL public tuyệt đối của file hợp đồng — FE dùng thẳng để xem/tải.</summary>
    public string? DocumentViewUrl { get; set; }
    /// <summary>drafted | pending_otp | confirmed | cancelled</summary>
    public string Status { get; set; } = null!;
    public DateTime? ConfirmedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public static EngagementContractSummary From(ContractModel c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        AgreedValue = c.AgreedValue,
        DocumentViewUrl = MediaUrl.Resolve(c.DocumentUrl),
        Status = c.Status.ToString(),
        ConfirmedAt = c.ConfirmedAt,
        CreatedAt = c.CreatedAt
    };
}

public class ProjectWorkingResponse
{
    public Guid Id { get; set; }
    public Guid ProjectShopOwnerId { get; set; }
    public string? ProjectName { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string? ProviderDisplayName { get; set; }
    /// <summary>null = thuê trực tiếp; có giá trị = qua marketplace.</summary>
    public Guid? ApplyId { get; set; }
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? RequestMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Mốc provider xin nghiệm thu — null nếu provider chưa báo xong việc.</summary>
    public DateTime? CompletionRequestedAt { get; set; }

    /// <summary>Ghi chú bàn giao provider gửi kèm khi xin nghiệm thu.</summary>
    public string? CompletionRequestNote { get; set; }

    /// <summary>
    /// true = provider đã báo xong việc và đang chờ owner bấm nghiệm thu.
    /// DERIVED (status 'accepted' + có completionRequestedAt) — không phải trạng thái lưu trong DB.
    /// FE dùng cờ này để hiện nút "Nghiệm thu" cho owner.
    /// </summary>
    public bool IsAwaitingAcceptance { get; set; }

    /// <summary>Mốc một bên đề nghị huỷ ngang — null nếu không có đề nghị nào đang treo.</summary>
    public DateTime? TerminationRequestedAt { get; set; }

    /// <summary>Bên đã gửi đề nghị huỷ ngang: "owner" | "provider" | null.</summary>
    public string? TerminationRequestedBy { get; set; }

    /// <summary>Lý do huỷ ngang bên đề nghị gửi kèm.</summary>
    public string? TerminationRequestNote { get; set; }

    /// <summary>Mốc hai bên chốt huỷ ngang — null nếu engagement chưa 'terminated'.</summary>
    public DateTime? TerminatedAt { get; set; }

    /// <summary>
    /// true = có đề nghị huỷ ngang đang chờ bên kia phản hồi.
    /// DERIVED (status 'accepted' + có terminationRequestedAt) — không phải trạng thái lưu trong DB.
    /// FE dùng cờ này để hiện nút "Đồng ý huỷ" / "Từ chối" cho bên còn lại.
    /// </summary>
    public bool IsAwaitingTerminationApproval { get; set; }

    /// <summary>
    /// Contract hiện hành CỦA RIÊNG engagement này — null nếu chưa lập hợp đồng.
    /// Ưu tiên bản 'confirmed', sau đó tới bản mới nhất chưa bị huỷ.
    /// </summary>
    public EngagementContractSummary? Contract { get; set; }

    /// <summary>
    /// true = engagement đã ký (có contract 'confirmed') — mốc mở khoá design/construction_item.
    /// FE dùng cờ này thay vì tự suy từ provider_status.
    /// </summary>
    public bool HasConfirmedContract { get; set; }

    public static ProjectWorkingResponse From(ProjectWorkingModel e)
    {
        // Contracts phải được Include; không Include thì coi như chưa có dữ liệu contract.
        var current = e.Contracts
            .Where(c => c.Status != ContractStatusEnum.cancelled)
            .OrderByDescending(c => c.Status == ContractStatusEnum.confirmed)
            .ThenByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        return new ProjectWorkingResponse
        {
            Id = e.Id,
            ProjectShopOwnerId = e.ProjectShopOwnerId,
            ProjectName = e.ProjectShopOwner?.Name,
            ServiceProviderProfileId = e.ServiceProviderProfileId,
            ProviderDisplayName = e.ServiceProviderProfile?.DisplayName,
            ApplyId = e.ApplyId,
            ContractType = e.ContractType.ToString(),
            Status = e.Status.ToString(),
            RequestMessage = e.RequestMessage,
            StartedAt = e.StartedAt,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            CompletionRequestedAt = e.CompletionRequestedAt,
            CompletionRequestNote = e.CompletionRequestNote,
            IsAwaitingAcceptance = e.Status == ProviderStatusEnum.accepted && e.CompletionRequestedAt != null,
            TerminationRequestedAt = e.TerminationRequestedAt,
            TerminationRequestedBy = e.TerminationRequestedBy?.ToString(),
            TerminationRequestNote = e.TerminationRequestNote,
            TerminatedAt = e.TerminatedAt,
            IsAwaitingTerminationApproval =
                e.Status == ProviderStatusEnum.accepted && e.TerminationRequestedAt != null,
            Contract = current != null ? EngagementContractSummary.From(current) : null,
            HasConfirmedContract = current?.Status == ContractStatusEnum.confirmed
        };
    }
}
