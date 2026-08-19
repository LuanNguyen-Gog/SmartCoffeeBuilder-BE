namespace SmartCoffeeBuilder.Service.DTOs.Requests.PaymentBatch;

/// <summary>
/// Gắn đợt thanh toán vào một hạng mục thi công (review 3: "xác nhận cho từng hạng mục đã thanh
/// toán"). Gửi null để gỡ liên kết.
/// </summary>
public class LinkConstructionItemRequest
{
    public Guid? ConstructionItemId { get; set; }
}
