namespace SmartCoffeeBuilder.Service.DTOs.Requests.DesignBrief;

public class UpdateDesignBriefRequest
{
    public string? TargetCustomer { get; set; }
    public string? Style { get; set; }
    public string? Mood { get; set; }
    public int? SeatCount { get; set; }
    public string? Timeline { get; set; }
    public string? BrandNote { get; set; }
    public string? BusinessModel { get; set; }
    public string? BusinessGoals { get; set; }
    public string? OperationNote { get; set; }
}
