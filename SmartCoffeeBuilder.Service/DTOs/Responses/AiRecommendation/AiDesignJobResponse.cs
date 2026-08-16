using System.Text.Json;
using System.Text.Json.Serialization;
using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;

public class AiDesignJobStatusResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = null!;
    
    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = null!;
    
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = null!;
    
    public string State { get; set; } = null!;
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("startedAt")]
    public DateTime? StartedAt { get; set; }
    
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }
    
    public int Attempts { get; set; }
    
    [JsonPropertyName("lastError")]
    public JsonElement? LastError { get; set; }
    
    [JsonPropertyName("parentJobId")]
    public string? ParentJobId { get; set; }
    
    // Plan fields
    public string? ConceptName { get; set; }
    public string? Summary { get; set; }
    
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
}

public class AiDesignArtifactResponse
{
    [JsonPropertyName("artifactId")]
    public string ArtifactId { get; set; } = null!;
    
    public string Kind { get; set; } = null!;
    public string Url { get; set; } = null!;
    
    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = null!;
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
