using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Design;

public class CreateDesignRequest
{
    [Required]
    public long ProjectWorkingId { get; set; }

    public string? Title { get; set; }

    /// <summary>concept | layout_2d | render_3d | technical_drawing</summary>
    [Required]
    public string Type { get; set; } = null!;

    // KHÔNG có CreatedBy: người tạo lấy từ JWT (xem CreateConstructionItemRequest).
}
