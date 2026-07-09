namespace SmartCoffeeBuilder.Service.Messaging;

/// <summary>
/// Wire-format DTOs exchanged with the AI design pipeline over RabbitMQ.
/// </summary>
public class AiDesignRequestMessage
{
    public long RecommendationId { get; set; }
    public long BriefId { get; set; }
    public string UserId { get; set; } = null!;
    public string ProjectId { get; set; } = null!;
    public AiDesignRequestPayload Payload { get; set; } = null!;
    public DateTime RequestedAt { get; set; }
}

public class AiDesignRequestPayload
{
    public string ShopName { get; set; } = null!;
    public string Location { get; set; } = null!;
    public double AreaSqm { get; set; }
    public int FloorCount { get; set; }
    public decimal BudgetVnd { get; set; }
    public List<string> BusinessGoals { get; set; } = new();
    public List<string> TargetCustomers { get; set; } = new();
    public string BusinessModel { get; set; } = null!;
    public string PrimaryStyle { get; set; } = null!;
    public List<string> BrandMoodKeywords { get; set; } = new();
    public List<string> MustHaveZones { get; set; } = new();
    public List<string> NiceToHaveZones { get; set; } = new();
    public int SeatTarget { get; set; }
    public List<string> ReferenceImageUrls { get; set; } = new();
    public string? Notes { get; set; }
    public bool GenerateImage { get; set; }
    public string? ImageView { get; set; }
    public string? DetailLevel { get; set; }
}

public class AiDesignResultMessage
{
    public long RecommendationId { get; set; }
    public string JobId { get; set; } = null!;
    public string State { get; set; } = null!;
    public string? Error { get; set; }

    // Plan fields
    public string? ConceptName { get; set; }
    public string? Summary { get; set; }

    // Layout
    public double? LayoutWidth { get; set; }
    public double? LayoutHeight { get; set; }
    public string? LayoutUnit { get; set; }
    public string? Zones { get; set; } // JSON
    public string? AdjacencyRules { get; set; } // JSON

    // Cost Estimate
    public decimal? FitoutMinVnd { get; set; }
    public decimal? FitoutMaxVnd { get; set; }
    public decimal? EquipmentMinVnd { get; set; }
    public decimal? EquipmentMaxVnd { get; set; }
    public decimal? ContingencyPercent { get; set; }
    public string? CostNotes { get; set; }

    // Customer Flow & Recommendations
    public string? CustomerFlow { get; set; } // JSON
    public string? Recommendations { get; set; } // JSON
    public string? RiskNotes { get; set; } // JSON

    // Image Spec
    public string? ImageView { get; set; }
    public string? ImagePrompt { get; set; }
    public string? ImageAspectRatio { get; set; }
    public string? ImageNegativePrompt { get; set; }
    public string? ImageReferenceUrls { get; set; } // JSON
    public string? ImageArtifactUrl { get; set; }

    // Seat capacity
    public int? SeatCapacityRecommendation { get; set; }

    public DateTime? CompletedAt { get; set; }
}