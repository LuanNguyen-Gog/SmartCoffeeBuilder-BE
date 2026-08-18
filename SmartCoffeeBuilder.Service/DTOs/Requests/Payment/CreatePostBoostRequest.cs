namespace SmartCoffeeBuilder.Service.DTOs.Requests.Payment;

/// <summary>Mua lượt đẩy bài đăng tuyển provider lên đầu danh sách trong Days ngày.</summary>
public class CreatePostBoostRequest
{
    public Guid PostId { get; set; }
    public int Days { get; set; }

    /// <summary>"web" (mặc định) hoặc "mobile" — quyết định cặp returnUrl/cancelUrl gửi cho payOS.</summary>
    public string? Platform { get; set; }
}
