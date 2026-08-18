using System.Text.Json.Serialization;
using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;

public class AiRecommendationResponse
{
    public Guid Id { get; set; }
    public Guid BriefId { get; set; }
    public string ConceptSummary { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public decimal? EstimatedDesignCost { get; set; }
    public decimal? EstimatedConstructionCost { get; set; }
    public DateTime CreatedAt { get; set; }

    // AI Design Job tracking
    public string? JobId { get; set; }
    public string? State { get; set; }
    public string? LastError { get; set; }
    public int Attempts { get; set; }
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
    public List<Zone>? LayoutZones { get; set; }
    public List<AdjacencyRule>? LayoutAdjacencyRules { get; set; }
    
    // Cost Estimate
    public decimal? FitoutMinVnd { get; set; }
    public decimal? FitoutMaxVnd { get; set; }
    public decimal? EquipmentMinVnd { get; set; }
    public decimal? EquipmentMaxVnd { get; set; }
    public decimal? ContingencyPercent { get; set; }
    public string? CostNotes { get; set; }
    
    // Customer Flow & Recommendations
    public List<CustomerFlow>? CustomerFlow { get; set; }
    public List<Recommendation>? Recommendations { get; set; }
    public List<RiskNote>? RiskNotes { get; set; }
    
    // Image Spec
    public string? ImageView { get; set; }
    public string? ImagePrompt { get; set; }
    public string? ImageAspectRatio { get; set; }
    public string? ImageNegativePrompt { get; set; }
    public List<string>? ImageReferenceUrls { get; set; }
    public string? ImageArtifactUrl { get; set; }
    
    // Seat capacity
    public int? SeatCapacityRecommendation { get; set; }

    public static AiRecommendationResponse From(SmartCoffeeBuilder.Repository.Models.AiRecommendation r) => new()
    {
        Id = r.Id,
        BriefId = r.BriefId,
        ConceptSummary = r.ConceptSummary,
        Payload = r.Payload,
        EstimatedDesignCost = r.EstimatedDesignCost,
        EstimatedConstructionCost = r.EstimatedConstructionCost,
        CreatedAt = r.CreatedAt,
        JobId = r.JobId,
        State = r.State,
        LastError = r.LastError,
        Attempts = r.Attempts,
        StartedAt = r.StartedAt,
        CompletedAt = r.CompletedAt,
        ParentJobId = r.ParentJobId,
        PlanConceptName = r.PlanConceptName,
        PlanSummary = r.PlanSummary,
        LayoutWidth = r.LayoutWidth,
        LayoutHeight = r.LayoutHeight,
        LayoutUnit = r.LayoutUnit,
        LayoutZones = DeserializeJson<List<Zone>>(r.LayoutZones),
        LayoutAdjacencyRules = DeserializeJson<List<AdjacencyRule>>(r.LayoutAdjacencyRules),
        FitoutMinVnd = r.FitoutMinVnd,
        FitoutMaxVnd = r.FitoutMaxVnd,
        EquipmentMinVnd = r.EquipmentMinVnd,
        EquipmentMaxVnd = r.EquipmentMaxVnd,
        ContingencyPercent = r.ContingencyPercent,
        CostNotes = r.CostNotes,
        CustomerFlow = DeserializeJson<List<CustomerFlow>>(r.CustomerFlow),
        Recommendations = DeserializeJson<List<Recommendation>>(r.Recommendations),
        RiskNotes = DeserializeJson<List<RiskNote>>(r.RiskNotes),
        ImageView = r.ImageView,
        ImagePrompt = r.ImagePrompt,
        ImageAspectRatio = r.ImageAspectRatio,
        ImageNegativePrompt = r.ImageNegativePrompt,
        ImageReferenceUrls = DeserializeJson<List<string>>(r.ImageReferenceUrls),
        ImageArtifactUrl = r.ImageArtifactUrl,
        SeatCapacityRecommendation = r.SeatCapacityRecommendation
    };

    private static T? DeserializeJson<T>(string? json) where T : class
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            // Worker publishes JSON with snake_case/lowercase keys
            // (e.g. {"id": "ZONE_1", "label": "...", "is_staff_only": false}).
            // Enable case-insensitive matching so PascalCase C# properties bind correctly.
            return System.Text.Json.JsonSerializer.Deserialize<T>(
                json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch
        {
            return null;
        }
    }
}
