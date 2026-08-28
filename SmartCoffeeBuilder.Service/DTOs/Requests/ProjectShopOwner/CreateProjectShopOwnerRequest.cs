using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;

public class CreateProjectShopOwnerRequest
{
    [Required]
    public Guid OwnerId { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Address { get; set; } = null!;

    /// <summary>
    /// Toạ độ mặt bằng khi chủ quán ghim trên bản đồ. Gửi CẢ HAI hoặc KHÔNG GỬI GÌ — một mình vĩ
    /// độ không chỉ tới đâu cả, server trả 400. Bỏ trống thì dự án chỉ có địa chỉ chữ, vẫn hợp lệ.
    /// </summary>
    public double? Latitude { get; set; }

    /// <inheritdoc cref="Latitude"/>
    public double? Longitude { get; set; }

    [Range(0, double.MaxValue)]
    public decimal AreaM2 { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Budget { get; set; }
}
