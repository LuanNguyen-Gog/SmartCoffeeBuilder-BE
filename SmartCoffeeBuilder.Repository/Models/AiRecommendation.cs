namespace SmartCoffeeBuilder.Repository.Models;

public class AiRecommendation
{
    public long Id { get; set; }
    public long BriefId { get; set; }
    public string ConceptSummary { get; set; } = null!;
    public string Payload { get; set; } = null!; // jsonb
    public decimal? EstimatedDesignCost { get; set; }
    public decimal? EstimatedConstructionCost { get; set; }
    public DateTime CreatedAt { get; set; }

    // AI Design Job tracking
    public string? JobId { get; set; }
    public string? State { get; set; } // queued, processing, completed, failed
    public string? LastError { get; set; }
    public int Attempts { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ParentJobId { get; set; }

    // Plan fields (individual columns for structured data)
    public string? PlanConceptName { get; set; }
    public string? PlanSummary { get; set; }
    
    // Layout Plan
    public double? LayoutWidth { get; set; }
    public double? LayoutHeight { get; set; }
    public string? LayoutUnit { get; set; }
    public string? LayoutZones { get; set; } // jsonb array of zones
    public string? LayoutAdjacencyRules { get; set; } // jsonb array
    
    // Cost Estimate
    public decimal? FitoutMinVnd { get; set; }
    public decimal? FitoutMaxVnd { get; set; }
    public decimal? EquipmentMinVnd { get; set; }
    public decimal? EquipmentMaxVnd { get; set; }
    public decimal? ContingencyPercent { get; set; }
    public string? CostNotes { get; set; }
    
    // Customer Flow & Recommendations
    public string? CustomerFlow { get; set; } // jsonb array
    public string? Recommendations { get; set; } // jsonb array
    public string? RiskNotes { get; set; } // jsonb array
    
    // Image Spec
    public string? ImageView { get; set; }
    public string? ImagePrompt { get; set; }
    public string? ImageAspectRatio { get; set; }
    public string? ImageNegativePrompt { get; set; }
    public string? ImageReferenceUrls { get; set; } // jsonb array
    public string? ImageArtifactUrl { get; set; }
    
    // Seat capacity
    public int? SeatCapacityRecommendation { get; set; }
    
    // Raw plan JSON for debugging (optional)
    public string? PlanJson { get; set; }

    public DesignBrief Brief { get; set; } = null!;
}
