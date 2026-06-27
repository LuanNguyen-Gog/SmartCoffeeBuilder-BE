using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;

public class AiRecommendationResponse
{
    public long Id { get; set; }
    public long BriefId { get; set; }
    public string ConceptSummary { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public decimal? EstimatedDesignCost { get; set; }
    public decimal? EstimatedConstructionCost { get; set; }
    public DateTime CreatedAt { get; set; }

    public static AiRecommendationResponse From(SmartCoffeeBuilder.Repository.Models.AiRecommendation r) => new()
    {
        Id = r.Id,
        BriefId = r.BriefId,
        ConceptSummary = r.ConceptSummary,
        Payload = r.Payload,
        EstimatedDesignCost = r.EstimatedDesignCost,
        EstimatedConstructionCost = r.EstimatedConstructionCost,
        CreatedAt = r.CreatedAt
    };
}
