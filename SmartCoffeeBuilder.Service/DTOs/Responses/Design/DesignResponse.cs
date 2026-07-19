namespace SmartCoffeeBuilder.Service.DTOs.Responses.Design;

public class DesignResponse
{
    public long Id { get; set; }
    public long ProjectWorkingId { get; set; }
    public string? Title { get; set; }
    public decimal Version { get; set; }
    public string Type { get; set; } = null!;
    /// <summary>Lý do revision gần nhất do owner yêu cầu.</summary>
    public string? Reason { get; set; }
    public string Status { get; set; } = null!;
    public long? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<DesignImageResponse> Images { get; set; } = [];

    public static DesignResponse From(SmartCoffeeBuilder.Repository.Models.Design e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        Title = e.Title,
        Version = e.Version,
        Type = e.Type.ToString(),
        Reason = e.Reason,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Images = e.DesignImages.Select(DesignImageResponse.From).ToList()
    };
}
