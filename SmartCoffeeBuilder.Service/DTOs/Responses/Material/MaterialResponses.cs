namespace SmartCoffeeBuilder.Service.DTOs.Responses.Material;

/// <summary>Một dòng trong bảng giá vật tư của engagement.</summary>
public class MaterialResponse
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Unit { get; set; } = null!;
    public decimal UnitPrice { get; set; }
    public int SortOrder { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static MaterialResponse From(SmartCoffeeBuilder.Repository.Models.Material e) => new()
    {
        Id = e.Id,
        ProjectWorkingId = e.ProjectWorkingId,
        Name = e.Name,
        Description = e.Description,
        Unit = e.Unit.ToString(),
        UnitPrice = e.UnitPrice,
        SortOrder = e.SortOrder,
        CreatedBy = e.CreatedBy,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

/// <summary>Một dòng "hạng mục/task này dùng vật tư gì, bao nhiêu".</summary>
public class ConstructionMaterialResponse
{
    public Guid Id { get; set; }
    public Guid? ConstructionItemId { get; set; }
    public Guid? ConstructionTaskId { get; set; }
    public Guid MaterialId { get; set; }

    /// <summary>Tên vật tư — kèm sẵn để FE khỏi phải gọi thêm bảng giá.</summary>
    public string MaterialName { get; set; } = null!;
    public string Unit { get; set; } = null!;

    /// <summary>Đơn giá đã chốt lúc chọn vật tư (không đọc động từ bảng giá).</summary>
    public decimal UnitPrice { get; set; }

    public decimal EstimatedQuantity { get; set; }
    public decimal? ActualQuantity { get; set; }

    /// <summary>= EstimatedQuantity × UnitPrice.</summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>= ActualQuantity × UnitPrice. null khi chưa ghi nhận lượng thực tế.</summary>
    public decimal? ActualCost { get; set; }

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static ConstructionMaterialResponse From(
        SmartCoffeeBuilder.Repository.Models.ConstructionMaterial e) => new()
    {
        Id = e.Id,
        ConstructionItemId = e.ConstructionItemId,
        ConstructionTaskId = e.ConstructionTaskId,
        MaterialId = e.MaterialId,
        MaterialName = e.Material?.Name ?? string.Empty,
        Unit = e.Material?.Unit.ToString() ?? string.Empty,
        UnitPrice = e.UnitPrice,
        EstimatedQuantity = e.EstimatedQuantity,
        ActualQuantity = e.ActualQuantity,
        EstimatedCost = e.EstimatedQuantity * e.UnitPrice,
        ActualCost = e.ActualQuantity * e.UnitPrice,
        Note = e.Note,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}

/// <summary>
/// Tổng hợp chi phí vật tư của một milestone: phần khai thẳng ở milestone CỘNG phần của mọi task
/// con (review 3: "milestone có khối lượng và giá gộp từ các task của nó").
/// </summary>
public class MaterialCostSummaryResponse
{
    public Guid ConstructionItemId { get; set; }

    /// <summary>Chi phí dự tính của riêng milestone (vật tư khai trực tiếp).</summary>
    public decimal OwnEstimatedCost { get; set; }
    public decimal? OwnActualCost { get; set; }

    /// <summary>Chi phí dự tính cộng dồn từ mọi task con.</summary>
    public decimal TasksEstimatedCost { get; set; }
    public decimal? TasksActualCost { get; set; }

    /// <summary>Tổng = own + tasks.</summary>
    public decimal TotalEstimatedCost { get; set; }
    public decimal? TotalActualCost { get; set; }

    /// <summary>Số dòng vật tư còn thiếu lượng thực tế — tổng thực tế chỉ đọc được khi bằng 0.</summary>
    public int MissingActualCount { get; set; }

    public List<ConstructionMaterialResponse> Lines { get; set; } = new();
}
