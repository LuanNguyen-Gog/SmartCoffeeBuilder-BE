namespace SmartCoffeeBuilder.Service.DTOs.Responses.Admin;

/// <summary>Tổng số + phân rã theo trạng thái (dùng chung cho project/post/apply/engagement/contract).</summary>
public class CountByStatus
{
    public int Total { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
}
