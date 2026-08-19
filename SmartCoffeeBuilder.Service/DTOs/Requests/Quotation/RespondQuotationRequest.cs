namespace SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;

/// <summary>Lý do owner gửi kèm khi yêu cầu bản khác hoặc từ chối báo giá.</summary>
public class RespondQuotationRequest
{
    public string? Reason { get; set; }
}
