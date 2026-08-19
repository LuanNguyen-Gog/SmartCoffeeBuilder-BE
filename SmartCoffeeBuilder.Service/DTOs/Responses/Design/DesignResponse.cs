namespace SmartCoffeeBuilder.Service.DTOs.Responses.Design;

public class DesignResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public string? Title { get; set; }
    public decimal Version { get; set; }
    public string Type { get; set; } = null!;
    /// <summary>Lý do revision gần nhất do owner yêu cầu.</summary>
    public string? Reason { get; set; }
    /// <summary>Provider mô tả thay đổi so với bản trước.</summary>
    public string? ChangeSummary { get; set; }
    public string Status { get; set; } = null!;
    public Guid? CreatedBy { get; set; }
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
        ChangeSummary = e.ChangeSummary,
        Status = e.Status.ToString(),
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Images = e.DesignImages.Select(DesignImageResponse.From).ToList()
    };
}
