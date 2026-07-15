using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Post;

public class UpdatePostRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Description { get; set; }

    /// <summary>design | construction | both</summary>
    public string? ServiceKind { get; set; }

    /// <summary>open | closed | cancelled</summary>
    public string? Status { get; set; }

    public DateTime? SubmissionDeadline { get; set; }
}
