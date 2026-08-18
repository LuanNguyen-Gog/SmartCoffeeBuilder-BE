using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Lượt đăng ký gói phí nền tảng của một account.
/// StartDate/EndDate chỉ có ý nghĩa khi Status = active (được set lại lúc kích hoạt).
/// </summary>
public class Subscription
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid PlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.pending;
    public decimal PaidAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public SubscriptionPlan Plan { get; set; } = null!;
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
}
