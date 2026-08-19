using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một dòng trong BẢNG GIÁ VẬT TƯ của engagement (review 3: "giá vật tư phải được công bố trước
/// khi thi công… là một danh sách thêm vật tư và tiền trên mỗi đơn vị").
///
/// Bảng giá neo vào <see cref="ProjectWorking"/> chứ không để chung toàn hệ thống: đơn giá là thứ
/// hai bên chốt với nhau cho ĐÚNG dự án này, và còn thay đổi theo thời điểm. Neo theo engagement
/// thì mỗi dự án giữ được bảng giá đã công bố của nó, không bị một lần sửa giá làm lệch số liệu
/// của các dự án đã chạy xong.
///
/// <see cref="ConstructionMaterial"/> mới là chỗ ghi "hạng mục/task này dùng bao nhiêu"; ở đây chỉ
/// khai báo có vật tư gì và bao nhiêu tiền một đơn vị.
/// </summary>
public class Material
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>project_providers.id</c>. Bảng giá thuộc về đúng một engagement.</summary>
    public Guid ProjectWorkingId { get; set; }

    /// <summary>Tên vật tư: "Gạch men 60x60", "Sơn nội thất Dulux", "Bóng đèn LED downlight"…</summary>
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Đơn vị tính — quyết định ý nghĩa của <see cref="UnitPrice"/> và số lượng.</summary>
    public MaterialUnit Unit { get; set; }

    /// <summary>Đơn giá theo <see cref="Unit"/>. Giá trị tiền trong hệ thống chỉ mang tính tham khảo.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Thứ tự hiển thị — id là uuid nên KHÔNG suy ra được thứ tự nhập.</summary>
    public int SortOrder { get; set; }

    /// <summary>Account id người khai báo (provider) — lấy từ JWT.</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }

    /// <summary>Các lần vật tư này được hạng mục / task lấy ra dùng.</summary>
    public ICollection<ConstructionMaterial> Usages { get; set; } = new List<ConstructionMaterial>();
}
