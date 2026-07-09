using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Design;

public class RequestDesignRevisionRequest
{
    /// <summary>Lý do owner yêu cầu chỉnh sửa bản design.</summary>
    [Required]
    public string Reason { get; set; } = null!;
}
