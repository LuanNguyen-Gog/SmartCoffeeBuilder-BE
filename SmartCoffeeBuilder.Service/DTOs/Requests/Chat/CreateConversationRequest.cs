using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Chat;

/// <summary>Tạo thread mới trong engagement — topic rỗng thì service tự sinh "Thread #N".</summary>
public class CreateConversationRequest
{
    /// <summary>Engagement (ProjectWorking) id — phải do account đăng nhập làm owner hoặc provider.</summary>
    [Required]
    public long ProjectWorkingId { get; set; }

    /// <summary>Tên thread. Tối đa 200 ký tự; bỏ trống/khoảng trắng thì service tự đặt.</summary>
    [MaxLength(200)]
    public string? Topic { get; set; }
}
