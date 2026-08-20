using Entities = SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.SiteProfile;

/// <summary>Hồ sơ thông số vật lý của mặt bằng, kèm danh sách tầng và ô mở.</summary>
public class SiteProfileResponse
{
    public Guid Id { get; set; }
    public Guid ProjectShopOwnerId { get; set; }

    public decimal? LengthM { get; set; }
    public decimal? WidthM { get; set; }
    public decimal? FrontageWidthM { get; set; }
    public decimal? CeilingHeightM { get; set; }
    public decimal? RoadWidthM { get; set; }
    public string? Orientation { get; set; }
    public int? FloorCount { get; set; }
    public bool HasMezzanine { get; set; }
    public string? StructureNote { get; set; }
    public string? ExistingConditionNote { get; set; }

    /// <summary>
    /// Diện tích lô suy từ <see cref="LengthM"/> × <see cref="WidthM"/> — chỉ để đối chiếu với
    /// <c>projects.area_m2</c> owner tự khai. null khi thiếu một trong hai số đo.
    /// </summary>
    public decimal? DerivedFootprintM2 { get; set; }

    /// <summary>Tổng diện tích sàn cộng từ các tầng đã khai. null khi chưa tầng nào có diện tích.</summary>
    public decimal? TotalFloorAreaM2 { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<SiteFloorResponse> Floors { get; set; } = new();
    public List<SiteOpeningResponse> Openings { get; set; } = new();

    public static SiteProfileResponse From(Entities.SiteProfile e)
    {
        var floors = (e.Floors ?? new List<Entities.SiteFloor>())
            .OrderBy(f => f.FloorNo).Select(SiteFloorResponse.From).ToList();
        var areas = floors.Where(f => f.AreaM2.HasValue).Select(f => f.AreaM2!.Value).ToList();

        return new SiteProfileResponse
        {
            Id = e.Id,
            ProjectShopOwnerId = e.ProjectShopOwnerId,
            LengthM = e.LengthM,
            WidthM = e.WidthM,
            FrontageWidthM = e.FrontageWidthM,
            CeilingHeightM = e.CeilingHeightM,
            RoadWidthM = e.RoadWidthM,
            Orientation = e.Orientation?.ToString(),
            FloorCount = e.FloorCount,
            HasMezzanine = e.HasMezzanine,
            StructureNote = e.StructureNote,
            ExistingConditionNote = e.ExistingConditionNote,
            DerivedFootprintM2 = e.LengthM.HasValue && e.WidthM.HasValue
                ? Math.Round(e.LengthM.Value * e.WidthM.Value, 2)
                : null,
            TotalFloorAreaM2 = areas.Count == 0 ? null : areas.Sum(),
            CreatedBy = e.CreatedBy,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            Floors = floors,
            Openings = (e.Openings ?? new List<Entities.SiteOpening>())
                .OrderBy(o => o.SortOrder).Select(SiteOpeningResponse.From).ToList()
        };
    }
}

public class SiteFloorResponse
{
    public Guid Id { get; set; }
    public Guid SiteProfileId { get; set; }
    public int FloorNo { get; set; }
    public string? Name { get; set; }
    public decimal? AreaM2 { get; set; }
    public decimal? CeilingHeightM { get; set; }
    public string? Purpose { get; set; }
    public string? Note { get; set; }

    public static SiteFloorResponse From(Entities.SiteFloor e) => new()
    {
        Id = e.Id,
        SiteProfileId = e.SiteProfileId,
        FloorNo = e.FloorNo,
        Name = e.Name,
        AreaM2 = e.AreaM2,
        CeilingHeightM = e.CeilingHeightM,
        Purpose = e.Purpose,
        Note = e.Note
    };
}

public class SiteOpeningResponse
{
    public Guid Id { get; set; }
    public Guid SiteProfileId { get; set; }
    public Guid? SiteFloorId { get; set; }
    public string Type { get; set; } = null!;
    public string? Orientation { get; set; }
    public decimal? WidthM { get; set; }
    public decimal? HeightM { get; set; }
    public int Quantity { get; set; }
    public string? Note { get; set; }
    public int SortOrder { get; set; }

    public static SiteOpeningResponse From(Entities.SiteOpening e) => new()
    {
        Id = e.Id,
        SiteProfileId = e.SiteProfileId,
        SiteFloorId = e.SiteFloorId,
        Type = e.Type.ToString(),
        Orientation = e.Orientation?.ToString(),
        WidthM = e.WidthM,
        HeightM = e.HeightM,
        Quantity = e.Quantity,
        Note = e.Note,
        SortOrder = e.SortOrder
    };
}
