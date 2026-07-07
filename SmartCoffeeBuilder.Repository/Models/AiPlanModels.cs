using System.Text.Json.Serialization;

namespace SmartCoffeeBuilder.Repository.Models;

public class LayoutPlan
{
    public Canvas Canvas { get; set; } = null!;
    public List<Zone> Zones { get; set; } = new();
    public List<AdjacencyRule> AdjacencyRules { get; set; } = new();
}

public class Canvas
{
    public double Width { get; set; }
    public double Height { get; set; }
    public string Unit { get; set; } = "meter";
}

public class Zone
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("label")]
    public string? Label { get; set; }
    [JsonPropertyName("purpose")]
    public string? Purpose { get; set; }
    [JsonPropertyName("x")]
    public double X { get; set; }
    [JsonPropertyName("y")]
    public double Y { get; set; }
    [JsonPropertyName("w")]
    public double W { get; set; }
    [JsonPropertyName("h")]
    public double H { get; set; }
    [JsonPropertyName("is_staff_only")]
    public bool IsStaffOnly { get; set; }
}

public class AdjacencyRule
{
    [JsonPropertyName("from")]
    public string? From { get; set; }
    [JsonPropertyName("to")]
    public string? To { get; set; }
    [JsonPropertyName("relation")]
    public string? Relation { get; set; }
}

public class CostEstimate
{
    public decimal FitoutMinVnd { get; set; }
    public decimal FitoutMaxVnd { get; set; }
    public decimal EquipmentMinVnd { get; set; }
    public decimal EquipmentMaxVnd { get; set; }
    public decimal ContingencyPercent { get; set; }
    public string? Notes { get; set; }
}

public class CustomerFlow
{
    [JsonPropertyName("stage")]
    public string? Stage { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class Recommendation
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("rationale")]
    public string? Rationale { get; set; }
    [JsonPropertyName("priority")]
    public int Priority { get; set; }
}

public class RiskNote
{
    [JsonPropertyName("level")]
    public string? Level { get; set; }
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("mitigation")]
    public string? Mitigation { get; set; }
}

public class ImageSpec
{
    public string View { get; set; } = null!;
    public string Prompt { get; set; } = null!;
    public string AspectRatio { get; set; } = null!;
    public string? NegativePrompt { get; set; }
    public List<string> ReferenceImageUrls { get; set; } = new();
}
