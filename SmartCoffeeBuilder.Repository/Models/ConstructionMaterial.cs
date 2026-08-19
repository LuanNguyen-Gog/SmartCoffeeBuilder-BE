namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một dòng "hạng mục / task này dùng vật tư gì, bao nhiêu" (review 3: "mỗi task và giai đoạn phải
/// ước tính và ghi nhận lượng vật tư thực tế; lượng thực tế được điền sau khi hoàn thành").
///
/// Neo vào ĐÚNG MỘT trong hai — enforce bằng CHECK <c>ck_construction_materials_target</c>:
/// <see cref="ConstructionTaskId"/> (vật tư của một việc cụ thể) hoặc
/// <see cref="ConstructionItemId"/> (vật tư tính thẳng ở mức milestone, khi milestone không chia task).
/// Tổng của milestone = vật tư khai trực tiếp ở milestone + vật tư của mọi task con.
///
/// <see cref="UnitPrice"/> là BẢN SAO đơn giá tại thời điểm chọn vật tư, không đọc động từ
/// <see cref="Material"/>: bảng giá sửa về sau thì các hạng mục đã chốt phải giữ nguyên con số đã
/// công bố, nếu không thì mọi báo cáo chi phí cũ tự đổi số sau lưng người dùng.
/// </summary>
public class ConstructionMaterial
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>construction_items.id</c>. null khi dòng này thuộc một task.</summary>
    public Guid? ConstructionItemId { get; set; }

    /// <summary>FK -> <c>construction_tasks.id</c>. null khi dòng này khai thẳng ở milestone.</summary>
    public Guid? ConstructionTaskId { get; set; }

    /// <summary>FK -> <c>materials.id</c>. Vật tư phải có trong bảng giá đã công bố của engagement.</summary>
    public Guid MaterialId { get; set; }

    /// <summary>Lượng DỰ TÍNH — khai lúc lập kế hoạch, trước khi làm.</summary>
    public decimal EstimatedQuantity { get; set; }

    /// <summary>
    /// Lượng THỰC TẾ đã dùng. null tới khi công việc xong — đây là con số điền sau, dùng để
    /// đối chiếu với dự tính.
    /// </summary>
    public decimal? ActualQuantity { get; set; }

    /// <summary>Đơn giá chốt tại thời điểm chọn vật tư (bản sao của <c>materials.unit_price</c>).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Ghi chú riêng cho dòng này (lý do lệch dự tính, quy cách…).</summary>
    public string? Note { get; set; }

    /// <summary>Account id người khai — lấy từ JWT.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ConstructionItem? ConstructionItem { get; set; }
    public ConstructionTask? ConstructionTask { get; set; }
    public Material Material { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
}
