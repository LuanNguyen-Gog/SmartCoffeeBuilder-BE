using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.SiteProfile;

/// <summary>
/// Tạo hồ sơ thông số mặt bằng cho một dự án (1-1). Mọi số đo đều tuỳ chọn — hồ sơ điền dần,
/// thường bổ sung sau khi provider đi khảo sát.
/// </summary>
public class CreateSiteProfileRequest
{
    [Required]
    public Guid ProjectShopOwnerId { get; set; }

    public decimal? LengthM { get; set; }
    public decimal? WidthM { get; set; }
    public decimal? FrontageWidthM { get; set; }
    public decimal? CeilingHeightM { get; set; }
    public decimal? RoadWidthM { get; set; }

    /// <summary>north | northeast | east | southeast | south | southwest | west | northwest.</summary>
    public string? Orientation { get; set; }

    public int? FloorCount { get; set; }
    public bool HasMezzanine { get; set; }
    public string? StructureNote { get; set; }
    public string? ExistingConditionNote { get; set; }

    /// <summary>Khai luôn danh sách tầng ngay lúc tạo (tuỳ chọn).</summary>
    public List<SiteFloorRequest>? Floors { get; set; }

    /// <summary>Khai luôn cửa / ban công ngay lúc tạo (tuỳ chọn).</summary>
    public List<SiteOpeningRequest>? Openings { get; set; }
}

/// <summary>Cập nhật hồ sơ mặt bằng. Field null = giữ nguyên.</summary>
public class UpdateSiteProfileRequest
{
    public decimal? LengthM { get; set; }
    public decimal? WidthM { get; set; }
    public decimal? FrontageWidthM { get; set; }
    public decimal? CeilingHeightM { get; set; }
    public decimal? RoadWidthM { get; set; }
    public string? Orientation { get; set; }
    public int? FloorCount { get; set; }
    public bool? HasMezzanine { get; set; }
    public string? StructureNote { get; set; }
    public string? ExistingConditionNote { get; set; }
}

/// <summary>Một tầng của mặt bằng.</summary>
public class SiteFloorRequest
{
    /// <summary>1 = trệt, 2 = lầu 1…; số âm = hầm; 0 = gác lửng. Không trùng trong cùng mặt bằng.</summary>
    public int FloorNo { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    public decimal? AreaM2 { get; set; }
    public decimal? CeilingHeightM { get; set; }

    [MaxLength(255)]
    public string? Purpose { get; set; }

    public string? Note { get; set; }
}

/// <summary>Một ô mở: cửa chính/phụ/phục vụ, cửa sổ, ban công, sân thượng, giếng trời.</summary>
public class SiteOpeningRequest
{
    /// <summary>main_door | secondary_door | service_door | window | balcony | terrace | skylight.</summary>
    [Required]
    public string Type { get; set; } = null!;

    /// <summary>Tầng chứa ô mở này — bỏ trống nếu chưa xác định.</summary>
    public Guid? SiteFloorId { get; set; }

    /// <summary>Số hiệu tầng, dùng khi tạo cùng lúc với tầng (chưa có id). Ưu tiên SiteFloorId nếu có cả hai.</summary>
    public int? FloorNo { get; set; }

    public string? Orientation { get; set; }
    public decimal? WidthM { get; set; }
    public decimal? HeightM { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Note { get; set; }
    public int? SortOrder { get; set; }
}
