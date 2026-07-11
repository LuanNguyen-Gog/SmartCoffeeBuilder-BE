using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Design;

public class CreateDesignRequest
{
    [Required]
    public long ProjectProviderId { get; set; }

    public string? Title { get; set; }

    /// <summary>concept | layout_2d | render_3d | technical_drawing</summary>
    [Required]
    public string Type { get; set; } = null!;

    /// <summary>Account id của người tạo (provider).</summary>
    public long? CreatedBy { get; set; }
}
