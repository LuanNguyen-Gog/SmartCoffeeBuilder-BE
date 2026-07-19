using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Post;

public class CreatePostRequest
{
    [Required]
    public long ProjectShopOwnerId { get; set; }

    /// <summary>design | construction | both</summary>
    [Required]
    public string ServiceKind { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    [Required]
    public string Description { get; set; } = null!;

    public DateTime? SubmissionDeadline { get; set; }
}
