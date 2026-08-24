namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// Nhật ký thi công hằng ngày (review 3: "Bổ sung thêm Daily log để constructor cập nhật báo cáo
/// tiến độ hằng ngày") — mỗi bản ghi là một buổi làm việc: ngày thực hiện, nội dung đã làm,
/// hình ảnh/video hiện trường (<see cref="Media"/>) và vấn đề phát sinh nếu có.
///
/// Neo CỨNG vào <see cref="ProjectWorking"/> chứ không vào task, vì hai lý do:
/// <list type="bullet">
/// <item>Có ngày công trường chạy mà chưa quy được về task nào (dọn mặt bằng, chờ vật tư, mưa) —
/// bắt buộc gắn task thì những ngày đó không ai ghi, đúng lúc cần vết nhất.</item>
/// <item>Quyền đọc/ghi suy ra từ engagement; neo vào task thì phải join hai cấp mới biết ai được xem.</item>
/// </list>
/// <see cref="ConstructionTaskId"/> / <see cref="ConstructionItemId"/> là TUỲ CHỌN — điền vào thì
/// nhật ký hiện luôn trong chi tiết task/hạng mục đó (review 3: "link phần này với phần task để
/// biểu thị chi tiết quá trình làm task đó").
///
/// KHÁC <see cref="Issue"/>: issue là một sự cố có vòng đời riêng (open → resolved, có nguyên nhân,
/// giải pháp, ảnh xác nhận). <see cref="IssueNote"/> ở đây chỉ là dòng ghi chú trong báo cáo ngày —
/// thấy nó nghiêm trọng thì mới mở <c>Issue</c> riêng.
/// </summary>
public class DailyLog
{
    public Guid Id { get; set; }

    /// <summary>FK -> <c>project_providers.id</c> (cột <c>project_provider_id</c>), cascade theo engagement.</summary>
    public Guid ProjectWorkingId { get; set; }

    /// <summary>
    /// FK -> <c>construction_items.id</c>. null = báo cáo chung của ngày, chưa quy về hạng mục nào.
    /// Xoá hạng mục chỉ gỡ liên kết (SetNull) — nhật ký là vết công trường, không xoá theo.
    /// </summary>
    public Guid? ConstructionItemId { get; set; }

    /// <summary>
    /// FK -> <c>construction_tasks.id</c>. null = chưa gắn task cụ thể. Cũng SetNull khi xoá task.
    /// </summary>
    public Guid? ConstructionTaskId { get; set; }

    /// <summary>
    /// Ngày thực hiện công việc — KHÁC <see cref="CreatedAt"/> (thời điểm bấm lưu): đội thi công
    /// thường ghi bù cuối tuần, nên hai mốc này không thay nhau được.
    /// </summary>
    public DateOnly LogDate { get; set; }

    /// <summary>Nội dung công việc đã làm trong ngày. Bắt buộc — nhật ký trống không có giá trị.</summary>
    public string WorkDone { get; set; } = null!;

    /// <summary>Vấn đề phát sinh trong ngày (nếu có). null = ngày làm việc trơn tru.</summary>
    public string? IssueNote { get; set; }

    /// <summary>Thời tiết / điều kiện công trường — lý do chậm hay gặp nhất ở công trình ngoài trời.</summary>
    public string? WeatherNote { get; set; }

    /// <summary>Số nhân công có mặt trong ngày. null = không ghi nhận.</summary>
    public int? WorkerCount { get; set; }

    /// <summary>Account id người ghi nhật ký (nhà cung cấp đang thi công).</summary>
    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public ConstructionItem? ConstructionItem { get; set; }
    public ConstructionTask? ConstructionTask { get; set; }
    public Account? CreatedByAccount { get; set; }

    /// <summary>Ảnh/video hiện trường đính kèm báo cáo ngày.</summary>
    public ICollection<DailyLogMedia> Media { get; set; } = new List<DailyLogMedia>();
}
