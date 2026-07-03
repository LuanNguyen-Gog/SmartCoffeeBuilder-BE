namespace SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;

public class UpdateAiRecommendationRequest
{
    public string? ConceptSummary { get; set; }
    public string? Payload { get; set; }
    public decimal? EstimatedDesignCost { get; set; }
    public decimal? EstimatedConstructionCost { get; set; }
}
