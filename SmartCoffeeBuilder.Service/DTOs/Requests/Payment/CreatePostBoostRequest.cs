namespace SmartCoffeeBuilder.Service.DTOs.Requests.Payment;

/// <summary>Mua lượt đẩy bài đăng tuyển provider lên đầu danh sách trong Days ngày.</summary>
public class CreatePostBoostRequest
{
    public long PostId { get; set; }
    public int Days { get; set; }
}
