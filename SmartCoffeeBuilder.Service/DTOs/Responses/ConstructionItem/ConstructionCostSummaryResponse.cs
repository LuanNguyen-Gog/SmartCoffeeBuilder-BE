namespace SmartCoffeeBuilder.Service.DTOs.Responses.ConstructionItem;

/// <summary>
/// Chi phí của một hạng mục thi công (review 1.1: "quản lý thi công theo … chi phí").
///
/// Chi phí gồm HAI nguồn tách bạch, cộng lại mới ra tổng:
/// <list type="bullet">
/// <item><b>Nhân công / thiết bị</b> — cột <c>estimated_labor_cost</c> / <c>actual_labor_cost</c>
/// trên chính hạng mục và trên từng task con.</item>
/// <item><b>Vật tư</b> — suy từ <c>construction_materials</c>: lượng × đơn giá đã chốt.</item>
/// </list>
///
/// <c>Actual*</c> là <c>null</c> chứ không phải 0 khi chưa đủ số liệu thực tế — "chưa biết" khác
/// "bằng không", và một tổng 0 giả sẽ làm mọi so sánh dự toán/thực chi sai lệch.
/// </summary>
public class ConstructionCostSummaryResponse
{
    public Guid ConstructionItemId { get; set; }
    public string Name { get; set; } = null!;
    public string? Category { get; set; }
    public string Status { get; set; } = null!;

    // ── Của riêng hạng mục này (kể cả task con, KHÔNG kể milestone con) ──────────────

    /// <summary>Nhân công/thiết bị dự tính: của hạng mục + của mọi task con.</summary>
    public decimal EstimatedLaborCost { get; set; }

    /// <summary>Nhân công/thiết bị thực chi. null khi còn dòng chưa điền.</summary>
    public decimal? ActualLaborCost { get; set; }

    /// <summary>Vật tư dự tính: khai thẳng ở hạng mục + khai trong task con.</summary>
    public decimal EstimatedMaterialCost { get; set; }

    /// <summary>Vật tư thực dùng. null khi còn dòng vật tư chưa có lượng thực tế.</summary>
    public decimal? ActualMaterialCost { get; set; }

    /// <summary>= nhân công + vật tư, phần của riêng hạng mục này.</summary>
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }

    // ── Gộp cả milestone con ────────────────────────────────────────────────────────

    public decimal ChildrenEstimatedCost { get; set; }
    public decimal? ChildrenActualCost { get; set; }

    /// <summary>= EstimatedCost + ChildrenEstimatedCost. Con số dùng để báo cáo.</summary>
    public decimal TotalEstimatedCost { get; set; }
    public decimal? TotalActualCost { get; set; }

    /// <summary>
    /// Chênh lệch thực chi so với dự toán (dương = vượt dự toán). null khi chưa đủ số liệu thực tế.
    /// </summary>
    public decimal? Variance { get; set; }

    /// <summary>Số dòng vật tư còn thiếu lượng thực tế — lý do khiến các trường Actual* bị null.</summary>
    public int MissingActualMaterialLines { get; set; }

    /// <summary>Số hạng mục/task còn thiếu chi phí nhân công thực chi.</summary>
    public int MissingActualLaborLines { get; set; }

    // ── Lịch, kèm sẵn để báo cáo chi phí và tiến độ đọc chung một chỗ ────────────────

    public DateOnly? StartAt { get; set; }
    public DateOnly? EstimateAt { get; set; }
    public int? PlannedDurationDays { get; set; }
    public int? ActualDurationDays { get; set; }

    /// <summary>Milestone con, mỗi cái là một bản tóm tắt đầy đủ như trên (cây thi công chỉ 2 cấp).</summary>
    public List<ConstructionCostSummaryResponse> Children { get; set; } = new();
}

/// <summary>Chi phí thi công của CẢ hợp tác — cộng từ mọi milestone gốc.</summary>
public class EngagementCostSummaryResponse
{
    public Guid ProjectWorkingId { get; set; }

    public decimal EstimatedLaborCost { get; set; }
    public decimal? ActualLaborCost { get; set; }
    public decimal EstimatedMaterialCost { get; set; }
    public decimal? ActualMaterialCost { get; set; }

    public decimal TotalEstimatedCost { get; set; }
    public decimal? TotalActualCost { get; set; }
    public decimal? Variance { get; set; }

    public int MissingActualMaterialLines { get; set; }
    public int MissingActualLaborLines { get; set; }

    /// <summary>Số milestone gốc đã cộng vào tổng.</summary>
    public int RootItemCount { get; set; }

    /// <summary>Chi tiết từng milestone gốc (kèm milestone con của nó).</summary>
    public List<ConstructionCostSummaryResponse> Items { get; set; } = new();
}
