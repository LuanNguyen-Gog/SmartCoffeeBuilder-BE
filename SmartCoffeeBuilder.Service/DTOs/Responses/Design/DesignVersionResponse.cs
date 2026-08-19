using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Design;

public class DesignVersionResponse
{
    public Guid Id { get; set; }
    public Guid DesignId { get; set; }

    /// <summary>submitted | approved.</summary>
    public string SnapshotKind { get; set; } = null!;

    /// <summary>Version decimal(4,1) của design tại thời điểm snapshot (0.1, 0.2…).</summary>
    public decimal Version { get; set; }

    public string? Title { get; set; }
    public string Type { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Reason { get; set; }
    /// <summary>Mô tả thay đổi so với bản trước, đóng băng tại thời điểm snapshot.</summary>
    public string? ChangeSummary { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? SnapshottedBy { get; set; }

    /// <summary>CreatedAt của design gốc — giữ lại cho lịch sử.</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime SnapshottedAt { get; set; }

    public List<DesignVersionImageResponse> Images { get; set; } = [];

    public static DesignVersionResponse From(DesignVersion v) => new()
    {
        Id = v.Id,
        DesignId = v.DesignId,
        SnapshotKind = v.SnapshotKind.ToString(),
        Version = v.Version,
        Title = v.Title,
        Type = v.Type.ToString(),
        Status = v.Status.ToString(),
        Reason = v.Reason,
        ChangeSummary = v.ChangeSummary,
        CreatedBy = v.CreatedBy,
        SnapshottedBy = v.SnapshottedBy,
        CreatedAt = v.CreatedAt,
        SnapshottedAt = v.SnapshottedAt,
        Images = v.Images.Select(DesignVersionImageResponse.From).ToList()
    };
}