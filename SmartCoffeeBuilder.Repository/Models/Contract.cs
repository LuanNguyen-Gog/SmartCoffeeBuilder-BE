using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Contract
{
    public long Id { get; set; }
    public long ProjectProviderId { get; set; }
    public string Title { get; set; } = null!;
    public string? PartyInfo { get; set; }
    public string? Terms { get; set; }
    public decimal? AgreedValue { get; set; }
    public string? DocumentUrl { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public long? ConfirmedBy { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.drafted;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectProvider ProjectProvider { get; set; } = null!;
    public Account? ConfirmedByAccount { get; set; }
}
