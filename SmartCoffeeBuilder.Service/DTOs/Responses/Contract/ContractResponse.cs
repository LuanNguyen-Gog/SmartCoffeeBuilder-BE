using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Contract;

public class ContractResponse
{
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public string Title { get; set; } = null!;
    public string? PartyInfo { get; set; }
    public string? Terms { get; set; }
    public decimal? AgreedValue { get; set; }
    public string? DocumentUrl { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public long? ConfirmedBy { get; set; }
    public ContractStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Không expose OtpCode ra ngoài — mã chỉ gửi qua email cho owner.
    public static ContractResponse From(SmartCoffeeBuilder.Repository.Models.Contract e) => new()
    {
        Id = e.Id,
        ProjectProviderId = e.ProjectProviderId,
        Title = e.Title,
        PartyInfo = e.PartyInfo,
        Terms = e.Terms,
        AgreedValue = e.AgreedValue,
        DocumentUrl = e.DocumentUrl,
        OtpExpiresAt = e.OtpExpiresAt,
        ConfirmedAt = e.ConfirmedAt,
        ConfirmedBy = e.ConfirmedBy,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
