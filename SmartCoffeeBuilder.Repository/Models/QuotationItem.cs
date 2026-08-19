namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Một dòng hạng mục trong báo giá. Bộ field lấy đúng theo review 3:
/// Tên - Mô tả - Đơn vị tính - Số lượng - Đơn giá - Thành tiền - Note.
/// VD: "Concept design | 1 gói | 15.000.000 VND".
/// </summary>
public class QuotationItem
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Đơn vị tính: gói, m2, bộ, ngày công…</summary>
    public string? Unit { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Thành tiền = <see cref="Quantity"/> × <see cref="UnitPrice"/>. Service tính lại khi ghi,
    /// không tin số client gửi lên (nếu không thì tổng hợp đồng có thể lệch với từng dòng).
    /// </summary>
    public decimal Amount { get; set; }

    public string? Note { get; set; }

    /// <summary>Thứ tự hiển thị do provider sắp — id là uuid nên KHÔNG suy ra được thứ tự nhập.</summary>
    public int SortOrder { get; set; }

    public Quotation Quotation { get; set; } = null!;
}
