using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.AiRecommendation;

public class CreateAiRecommendationRequest
{
    [Required]
    public Guid BriefId { get; set; }

    [Required]
    public string ConceptSummary { get; set; } = null!;

    [Required]
    public string Payload { get; set; } = null!;

    [Range(0, double.MaxValue)]
    public decimal? EstimatedDesignCost { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? EstimatedConstructionCost { get; set; }
}
