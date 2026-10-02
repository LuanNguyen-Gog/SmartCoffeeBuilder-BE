using SmartCoffeeBuilder.Service.DTOs.Responses.AiRecommendation;
using SmartCoffeeBuilder.Service.DTOs.Responses.Design;
using SmartCoffeeBuilder.Service.DTOs.Responses.DesignBrief;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

public class OverviewProjectSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;

    /// <summary>Toạ độ mặt bằng, null khi chưa ghim bản đồ. Luôn đi theo cặp.</summary>
    public double? Latitude { get; set; }

    /// <inheritdoc cref="Latitude"/>
    public double? Longitude { get; set; }
    public decimal AreaM2 { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = null!;

    public static OverviewProjectSummary From(SmartCoffeeBuilder.Repository.Models.ProjectShopOwner p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Address = p.Address,
        Latitude = p.Latitude,
        Longitude = p.Longitude,
        AreaM2 = p.AreaM2,
        Budget = p.Budget,
        Status = p.Status.ToString()
    };
}

/// <summary>
/// Tổng quan dự án cho provider sau bước AI — nội dung theo contract_type:
/// - Mọi engagement: các kết quả AI đã hoàn tất (từ 02/10/2026 bên thi công cũng nhận).
/// - Engagement có design (design/both): thêm brief.
/// - Engagement chỉ construction: thêm bản vẽ đã 'approved' của bên design.
/// </summary>
public class EngagementOverviewResponse
{
    public Guid ProjectWorkingId { get; set; }
    public string ContractType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public OverviewProjectSummary ProjectShopOwner { get; set; } = null!;

    /// <summary>Brief của owner — chỉ engagement có design; null nếu project chưa có brief.</summary>
    public DesignBriefResponse? Brief { get; set; }

    /// <summary>Kết quả AI (state=completed) của dự án — mọi contract_type.</summary>
    public List<AiRecommendationResponse>? AiRecommendations { get; set; }

    /// <summary>Bản vẽ đã 'approved' của project — chỉ engagement construction-only.</summary>
    public List<DesignResponse>? ApprovedDesigns { get; set; }
}
