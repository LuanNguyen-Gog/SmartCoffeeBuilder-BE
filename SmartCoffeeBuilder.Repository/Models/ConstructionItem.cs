using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ConstructionItem
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public Guid? ParentId { get; set; } // phân cấp
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Category { get; set; }

    /// <summary>
    /// Thứ tự do người dùng sắp — nhỏ hơn thì đứng trước, tính TRONG cùng một nhóm anh em
    /// (cùng <see cref="ProjectWorkingId"/> và cùng <see cref="ParentId"/>).
    ///
    /// Trước đây danh sách chỉ sắp theo <see cref="EstimateAt"/>, nên nhà thầu không có cách nào
    /// đổi thứ tự hiển thị mà không sửa hạn hoàn thành — mà hạn là dữ liệu nghiệp vụ, không phải
    /// nút sắp xếp. Cột này tách hai việc đó ra.
    ///
    /// Không đảm bảo liên tục: xoá một hạng mục để lại khoảng trống, và khoảng trống không sao vì
    /// chỉ thứ tự TƯƠNG ĐỐI có ý nghĩa.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Ngày DỰ KIẾN bắt đầu. Cùng với <see cref="EstimateAt"/> (hạn hoàn thành) mới ra được
    /// THỜI LƯỢNG của hạng mục — review 1.1 hỏi "thời lượng", mà một mốc kết thúc đơn lẻ chỉ trả
    /// lời được "deadline". null = chưa lên lịch.
    /// </summary>
    public DateOnly? StartAt { get; set; }

    /// <summary>Ngày THỰC TẾ bắt đầu — điền khi hạng mục chuyển sang in_progress.</summary>
    public DateOnly? ActualStartAt { get; set; }

    public DateOnly? EstimateAt { get; set; }
    public DateOnly? ActualAt { get; set; }

    /// <summary>
    /// Chi phí nhân công / thuê thiết bị DỰ TÍNH của riêng hạng mục này. Tách khỏi vật tư vì vật tư
    /// đã có bảng riêng (<see cref="ConstructionMaterial"/>) tính theo lượng × đơn giá; cộng hai
    /// nguồn mới ra tổng chi phí hạng mục.
    /// </summary>
    public decimal? EstimatedLaborCost { get; set; }

    /// <summary>Chi phí nhân công / thiết bị THỰC CHI — điền sau khi hạng mục xong.</summary>
    public decimal? ActualLaborCost { get; set; }

    // v5: bỏ IsDone — trạng thái hoàn thành chỉ đọc từ Status = completed (một nguồn sự thật).
    public ItemStatus Status { get; set; } = ItemStatus.pending;

    /// <summary>
    /// Hạng mục này đã được thanh toán chưa (review 3: "xác nhận cho từng hạng mục đã thanh toán").
    /// KHÔNG phải nguồn sự thật độc lập: PaymentBatchService bật cờ này khi đợt thanh toán gắn với
    /// hạng mục được provider xác nhận, và tắt khi đợt đó bị bác. Đọc chi tiết tiền bạc thì đi qua
    /// payment_batches — cột này chỉ để FE khỏi phải join mỗi lần vẽ danh sách hạng mục.
    /// </summary>
    public bool IsPaid { get; set; }

    /// <summary>
    /// Mẫu quy trình đã sinh ra hạng mục này (null = gõ tay). Chỉ là VẾT NGUỒN, không phải liên
    /// kết sống: áp mẫu vẫn là copy một lần, sửa mẫu về sau không đụng tới hạng mục đã sinh.
    ///
    /// Có cột này thì chủ quán mới trả lời được câu hỏi review 3 đặt ra — "nhà thầu đang theo quy
    /// trình nào" — mà trước đây chỉ phía nhà thầu biết: ApplyAsync chép hạng mục xong là quên
    /// luôn mẫu gốc. Mẫu bị xoá thì cột về null (SetNull), kế hoạch đã sinh vẫn còn nguyên.
    /// </summary>
    public Guid? SourceTemplateId { get; set; }

    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public ConstructionTemplate? SourceTemplate { get; set; }
    public ConstructionItem? Parent { get; set; }
    public ICollection<ConstructionItem> Children { get; set; } = new List<ConstructionItem>();
    public Account? CreatedByAccount { get; set; }
    public ICollection<ConstructionTask> Tasks { get; set; } = new List<ConstructionTask>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();

    /// <summary>Các đợt thanh toán của hợp đồng được gắn vào chính hạng mục này.</summary>
    public ICollection<PaymentBatch> PaymentBatches { get; set; } = new List<PaymentBatch>();

    /// <summary>Checklist nghiệm thu của hạng mục thi công này (review 3).</summary>
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

    /// <summary>
    /// Vật tư khai THẲNG ở mức milestone (khi milestone không chia task). Tổng vật tư của milestone
    /// còn phải cộng thêm phần khai trong từng task con — xem <see cref="ConstructionMaterial"/>.
    /// </summary>
    public ICollection<ConstructionMaterial> Materials { get; set; } = new List<ConstructionMaterial>();

    /// <summary>Các khoản phát sinh chi phí gắn với hạng mục này.</summary>
    public ICollection<ChangeOrder> ChangeOrders { get; set; } = new List<ChangeOrder>();
    public ICollection<DailyLog> DailyLogs { get; set; } = new List<DailyLog>();
}
