namespace SmartCoffeeBuilder.Service.DTOs.Responses.Admin;

/// <summary>Một giao dịch payOS (dùng cho danh sách drill-down trong báo cáo doanh thu).</summary>
public class RevenueTransactionResponse
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string Purpose { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Platform { get; set; } = null!;
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public static RevenueTransactionResponse From(SmartCoffeeBuilder.Repository.Models.PaymentTransaction t) => new()
    {
        Id = t.Id,
        AccountId = t.AccountId,
        Purpose = t.Purpose.ToString(),
        Status = t.Status.ToString(),
        Platform = t.Platform.ToString(),
        OrderCode = t.OrderCode,
        Amount = t.Amount,
        Description = t.Description,
        CreatedAt = t.CreatedAt
    };
}
