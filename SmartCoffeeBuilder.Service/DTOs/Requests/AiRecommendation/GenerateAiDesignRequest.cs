using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;

/// <summary>
/// Request to trigger AI design generation.
/// Most information is auto-populated from the associated Project and DesignBrief.
/// </summary>
public class GenerateAiDesignRequest
{
    [Required]
    public long BriefId { get; set; }
    
    /// <summary>
    /// Minimum zones required (defaults from common cafe zones)
    /// </summary>
    [MaxLength(20)]
    public List<string>? MustHaveZones { get; set; }

    /// <summary>
    /// Optional zones to include if space allows
    /// </summary>
    [MaxLength(20)]
    public List<string>? NiceToHaveZones { get; set; }

    /// <summary>
    /// Additional notes for AI design
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Whether to generate images (default: false)
    /// </summary>
    public bool GenerateImage { get; set; } = false;

    /// <summary>
    /// Image view type if generating images
    /// </summary>
    public ImageView? ImageView { get; set; }

    /// <summary>
    /// Detail level: low, medium, high (default: medium)
    /// </summary>
    public DetailLevel? DetailLevel { get; set; }

    /// <summary>
    /// Number of alternative designs (1-3, default: 1)
    /// </summary>
    [System.ComponentModel.DataAnnotations.Range(1, 3)]
    public int AlternativesCount { get; set; } = 1;

    /// <summary>
    /// Reference image URLs
    /// </summary>
    [MaxLength(8)]
    public List<string>? ReferenceImageUrls { get; set; }
}

public enum ImageView
{
    Isometric,
    TopDown,
    PerspectiveInterior,
    FrontElevation
}

public enum DetailLevel
{
    Low,
    Medium,
    High
}
