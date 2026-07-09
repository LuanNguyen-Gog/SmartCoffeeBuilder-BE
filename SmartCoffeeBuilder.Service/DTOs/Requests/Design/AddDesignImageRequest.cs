using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Design;

public class AddDesignImageRequest
{
    [Required]
    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    /// <summary>Account id của người upload (provider).</summary>
    public long? UploadedBy { get; set; }
}
