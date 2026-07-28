using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Chat;

/// <summary>Đổi tên thread — chỉ member của engagement (owner/provider) mới được gọi.</summary>
public class UpdateConversationRequest
{
    [MaxLength(200)]
    public string? Topic { get; set; }
}
