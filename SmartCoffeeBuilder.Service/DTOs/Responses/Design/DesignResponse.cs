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

    /// <summary>
    /// Số vòng owner ĐÃ yêu cầu sửa. So với free_revision_count của báo giá đã chốt để biết vòng
    /// tiếp theo còn miễn phí hay phát sinh phí (review 1.1).
    /// </summary>
    public int RevisionCount { get; set; }
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
        RevisionCount = e.RevisionCount,
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Images = e.DesignImages.Select(DesignImageResponse.From).ToList()
    };
}
