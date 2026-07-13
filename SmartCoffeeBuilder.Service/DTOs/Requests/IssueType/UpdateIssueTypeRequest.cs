using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.IssueType;

/// <summary>Cập nhật tên hiển thị. Code là khoá ổn định — không đổi.</summary>
public class UpdateIssueTypeRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = null!;
}
