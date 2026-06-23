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

    public DesignBrief Brief { get; set; } = null!;
}
