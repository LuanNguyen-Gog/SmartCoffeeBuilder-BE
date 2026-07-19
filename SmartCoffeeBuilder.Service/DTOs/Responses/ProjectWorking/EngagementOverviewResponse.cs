using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

public class OverviewProjectSummary
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal AreaM2 { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = null!;

    public static OverviewProjectSummary From(SmartCoffeeBuilder.Repository.Models.ProjectShopOwner p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Address = p.Address,
        AreaM2 = p.AreaM2,
        Budget = p.Budget,
        Status = p.Status.ToString()
    };
}

/// <summary>
/// Tổng quan dự án cho provider sau bước AI — nội dung theo contract_type:
/// - Engagement có design (design/both): brief + các kết quả AI đã hoàn tất.
/// - Engagement chỉ construction: xem bản vẽ đã 'approved' của bên design (không xem AI plan).
/// </summary>
public class EngagementOverviewResponse
{
    public long ProjectWorkingId { get; set; }
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public OverviewProjectSummary ProjectShopOwner { get; set; } = null!;

    /// <summary>Brief của owner — chỉ engagement có design; null nếu project chưa có brief.</summary>
    public DesignBriefResponse? Brief { get; set; }

    /// <summary>Kết quả AI (state=completed) — chỉ engagement có design.</summary>
    public List<AiRecommendationResponse>? AiRecommendations { get; set; }

    /// <summary>Bản vẽ đã 'approved' của project — chỉ engagement construction-only.</summary>
    public List<DesignResponse>? ApprovedDesigns { get; set; }
}
