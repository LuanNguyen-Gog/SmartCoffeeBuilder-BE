namespace SmartCoffeeBuilder.Service.DTOs.Requests.ConstructionItem;

/// <summary>
/// Sắp lại thứ tự của MỘT nhóm anh em milestone — cùng engagement, cùng cấp cha.
///
/// Gửi cả nhóm chứ không gửi từng bước "đổi chỗ A với B": client đã biết thứ tự cuối cùng nó muốn,
/// và nhận nguyên danh sách thì server không phải đoán phần còn lại đang ở đâu — hai tab mở song
/// song cũng không ghi đè nhau thành một thứ tự chẳng bên nào chọn.
/// </summary>
public class ReorderConstructionItemsRequest
{
    public Guid ProjectWorkingId { get; set; }

    /// <summary>
    /// null = nhóm milestone GỐC của engagement. Khác null = các milestone con của milestone này.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// TOÀN BỘ id của nhóm, theo thứ tự mong muốn (phần tử đầu đứng trên cùng).
    /// Thiếu hoặc thừa id đều bị từ chối — xem <see cref="ProjectWorkingId"/>.
    /// </summary>
    public List<Guid> ItemIds { get; set; } = new();
}
