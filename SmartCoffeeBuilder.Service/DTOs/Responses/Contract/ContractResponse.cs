using SmartCoffeeBuilder.Service.Utils;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Contract;

public class ContractResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public string Title { get; set; } = null!;
    public string? PartyInfo { get; set; }
    public string? Terms { get; set; }
    public decimal? AgreedValue { get; set; }
    /// <summary>ObjectName file hợp đồng trên bucket — giá trị lưu trong DB.</summary>
    public string? DocumentUrl { get; set; }
    /// <summary>URL public tuyệt đối của file hợp đồng — FE dùng thẳng để xem/tải.</summary>
    public string? DocumentViewUrl { get; set; }

    /// <summary>Ngày bắt đầu thực hiện theo hợp đồng.</summary>
    public DateOnly? ExecutionStartAt { get; set; }

    /// <summary>Ngày kết thúc thực hiện theo hợp đồng.</summary>
    public DateOnly? ExecutionEndAt { get; set; }

    /// <summary>
    /// Số ngày thực hiện, tính cả ngày đầu và ngày cuối. DERIVED từ hai mốc trên — không lưu DB
    /// để khỏi có hai nguồn sự thật lệch nhau. null khi thiếu một mốc.
    /// </summary>
    public int? ExecutionDurationDays { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    /// <summary>Báo giá nguồn của hợp đồng — null với hợp đồng lập tay theo luồng cũ.</summary>
    public Guid? QuotationId { get; set; }
    /// <summary>drafted | pending_otp | confirmed | cancelled</summary>
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Không expose OtpCode ra ngoài — mã chỉ gửi qua email cho owner.
    public static ContractResponse From(SmartCoffeeBuilder.Repository.Models.Contract e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        Title = e.Title,
        PartyInfo = e.PartyInfo,
        Terms = e.Terms,
        AgreedValue = e.AgreedValue,
        DocumentUrl = e.DocumentUrl,
        DocumentViewUrl = MediaUrl.Resolve(e.DocumentUrl),
        ExecutionStartAt = e.ExecutionStartAt,
        ExecutionEndAt = e.ExecutionEndAt,
        ExecutionDurationDays = ConstructionSchedule.DurationDays(e.ExecutionStartAt, e.ExecutionEndAt),
        OtpExpiresAt = e.OtpExpiresAt,
        ConfirmedAt = e.ConfirmedAt,
        ConfirmedBy = e.ConfirmedBy,
        QuotationId = e.QuotationId,
        Status = e.Status.ToString(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
