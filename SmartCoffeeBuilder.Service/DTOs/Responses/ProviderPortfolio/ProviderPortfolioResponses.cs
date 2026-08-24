using SmartCoffeeBuilder.Service.Utils;
using Entities = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProviderPortfolio;

/// <summary>Một dự án mẫu trong hồ sơ năng lực của nhà cung cấp (review 1.1).</summary>
public class ProviderPortfolioResponse
{
    public Guid Id { get; set; }
    public Guid ServiceProviderProfileId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>design | construction | both.</summary>
    public string Role { get; set; } = null!;

    public string? Style { get; set; }
    public string? Location { get; set; }
    public decimal? AreaM2 { get; set; }
    public decimal? ContractValue { get; set; }
    public DateOnly? CompletedAt { get; set; }
    public int? DurationDays { get; set; }

    /// <summary>Link video như đã lưu (YouTube giữ nguyên, file trên bucket là ObjectName).</summary>
    public string? VideoUrl { get; set; }

    /// <summary>URL xem được của video — link ngoài giữ nguyên, ObjectName được resolve thành URL public.</summary>
    public string? VideoViewUrl { get; set; }

    public string? CoverImageUrl { get; set; }

    /// <summary>URL public của ảnh bìa — FE dùng thẳng làm img src.</summary>
    public string? CoverImageViewUrl { get; set; }

    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ProviderPortfolioImageResponse> Images { get; set; } = new();

    public static ProviderPortfolioResponse From(Entities.ProviderPortfolio e) => new()
    {
        Id = e.Id,
        ServiceProviderProfileId = e.ServiceProviderProfileId,
        Title = e.Title,
        Description = e.Description,
        Role = e.Role.ToString(),
        Style = e.Style,
        Location = e.Location,
        AreaM2 = e.AreaM2,
        ContractValue = e.ContractValue,
        CompletedAt = e.CompletedAt,
        DurationDays = e.DurationDays,
        VideoUrl = e.VideoUrl,
        VideoViewUrl = MediaUrl.Resolve(e.VideoUrl),
        CoverImageUrl = e.CoverImageUrl,
        CoverImageViewUrl = MediaUrl.Resolve(e.CoverImageUrl),
        IsFeatured = e.IsFeatured,
        SortOrder = e.SortOrder,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Images = (e.Images ?? new List<Entities.ProviderPortfolioImage>())
            .OrderBy(i => i.SortOrder)
            .Select(ProviderPortfolioImageResponse.From)
            .ToList()
    };
}

public class ProviderPortfolioImageResponse
{
    public Guid Id { get; set; }
    public Guid ProviderPortfolioId { get; set; }
    public string ImageUrl { get; set; } = null!;

    /// <summary>URL public tuyệt đối — FE dùng thẳng làm img src.</summary>
    public string? ImageViewUrl { get; set; }

    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ProviderPortfolioImageResponse From(Entities.ProviderPortfolioImage e) => new()
    {
        Id = e.Id,
        ProviderPortfolioId = e.ProviderPortfolioId,
        ImageUrl = e.ImageUrl,
        ImageViewUrl = MediaUrl.Resolve(e.ImageUrl),
        Caption = e.Caption,
        SortOrder = e.SortOrder,
        CreatedAt = e.CreatedAt
    };
}
