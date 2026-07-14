using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.IssueType;

/// <summary>Tạo loại issue (lookup, admin quản lý danh mục).</summary>
public class CreateIssueTypeRequest
{
    /// <summary>Mã duy nhất, ví dụ: site_condition, material_delay.</summary>
    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = null!;
}
