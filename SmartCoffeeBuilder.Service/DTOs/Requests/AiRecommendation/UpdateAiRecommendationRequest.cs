namespace SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;

public class UpdateAiRecommendationRequest
{
    public string? ConceptSummary { get; set; }
    public string? Payload { get; set; }
    public decimal? EstimatedDesignCost { get; set; }
    public decimal? EstimatedConstructionCost { get; set; }

    // AI Design Job updates
    public string? JobId { get; set; }
    public string? State { get; set; }
    public string? LastError { get; set; }
    public int? Attempts { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ParentJobId { get; set; }

    // Plan fields
    public string? PlanConceptName { get; set; }
    public string? PlanSummary { get; set; }
    
    // Layout
    public double? LayoutWidth { get; set; }
    public double? LayoutHeight { get; set; }
    public string? LayoutUnit { get; set; }
    public string? LayoutZones { get; set; }
    public string? LayoutAdjacencyRules { get; set; }
    
    // Cost Estimate
    public decimal? FitoutMinVnd { get; set; }
    public decimal? FitoutMaxVnd { get; set; }
    public decimal? EquipmentMinVnd { get; set; }
    public decimal? EquipmentMaxVnd { get; set; }
    public decimal? ContingencyPercent { get; set; }
    public string? CostNotes { get; set; }
    
    // Customer Flow & Recommendations
    public string? CustomerFlow { get; set; }
    public string? Recommendations { get; set; }
    public string? RiskNotes { get; set; }
    
    // Image Spec
    public string? ImageView { get; set; }
    public string? ImagePrompt { get; set; }
    public string? ImageAspectRatio { get; set; }
    public string? ImageNegativePrompt { get; set; }
    public string? ImageReferenceUrls { get; set; }
    public string? ImageArtifactUrl { get; set; }
    
    // Seat capacity
    public int? SeatCapacityRecommendation { get; set; }
    
    // Raw plan JSON
    public string? PlanJson { get; set; }
}
