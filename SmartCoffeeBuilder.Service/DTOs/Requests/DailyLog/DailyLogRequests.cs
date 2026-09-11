using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.DailyLog;

/// <summary>Một file hiện trường đính kèm báo cáo ngày.</summary>
public class DailyLogMediaRequest
{
    /// <summary>ObjectName trên bucket (hoặc URL đầy đủ nếu là link ngoài).</summary>
    [Required]
    public string MediaUrl { get; set; } = null!;

    /// <summary>image | video. Bỏ trống = image.</summary>
    public string? MediaType { get; set; }

    public string? Caption { get; set; }
}

/// <summary>
/// Nhà cung cấp ghi nhật ký thi công của một ngày. Engagement suy ra từ hạng mục/task khi có,
/// nên chỉ cần <see cref="ProjectWorkingId"/> khi ghi báo cáo chung không gắn hạng mục nào.
/// </summary>
public class CreateDailyLogRequest
{
    /// <summary>
    /// Engagement của nhật ký. Bỏ trống được NẾU có <see cref="ConstructionItemId"/> hoặc
    /// <see cref="ConstructionTaskId"/> — service lấy engagement từ đó và chặn nếu lệch nhau.
    /// </summary>
    public Guid? ProjectWorkingId { get; set; }

    public Guid? ConstructionItemId { get; set; }

    public Guid? ConstructionTaskId { get; set; }

    /// <summary>Ngày thực hiện công việc (yyyy-MM-dd). Bỏ trống = hôm nay.</summary>
    public DateOnly? LogDate { get; set; }

    /// <summary>Nội dung công việc đã làm trong ngày.</summary>
    [Required]
    public string WorkDone { get; set; } = null!;

    /// <summary>Vấn đề phát sinh (nếu có).</summary>
    public string? IssueNote { get; set; }

    /// <summary>Thời tiết / điều kiện công trường.</summary>
    public string? WeatherNote { get; set; }

    public int? WorkerCount { get; set; }

    /// <summary>Ảnh/video hiện trường. Thứ tự trong mảng chính là thứ tự hiển thị.</summary>
    public List<DailyLogMediaRequest>? Media { get; set; }
}

/// <summary>
/// Sửa nhật ký. Field null = giữ nguyên. Riêng <see cref="Media"/> khác null thì THAY TOÀN BỘ
/// danh sách file — gửi mảng rỗng để gỡ hết.
/// </summary>
public class UpdateDailyLogRequest
{
    private Guid? _constructionItemId;
    private Guid? _constructionTaskId;

    /// <summary>
    /// Hạng mục neo nhật ký. Gửi <c>null</c> TƯỜNG MINH = gỡ liên kết; bỏ hẳn field = giữ nguyên.
    /// System.Text.Json chỉ gọi setter khi field CÓ MẶT trong payload, kể cả khi giá trị là null —
    /// nhờ vậy phân biệt được hai ý định mà kiểu <c>Guid?</c> tự nó không diễn đạt được.
    /// </summary>
    public Guid? ConstructionItemId
    {
        get => _constructionItemId;
        set { _constructionItemId = value; HasConstructionItemId = true; }
    }

    /// <summary>Task neo nhật ký. Cùng quy ước null tường minh như <see cref="ConstructionItemId"/>.</summary>
    public Guid? ConstructionTaskId
    {
        get => _constructionTaskId;
        set { _constructionTaskId = value; HasConstructionTaskId = true; }
    }

    /// <summary>True khi payload CÓ field constructionItemId (kể cả giá trị null).</summary>
    [JsonIgnore]
    public bool HasConstructionItemId { get; private set; }

    /// <summary>True khi payload CÓ field constructionTaskId (kể cả giá trị null).</summary>
    [JsonIgnore]
    public bool HasConstructionTaskId { get; private set; }

    public DateOnly? LogDate { get; set; }
    public string? WorkDone { get; set; }
    public string? IssueNote { get; set; }
    public string? WeatherNote { get; set; }
    public int? WorkerCount { get; set; }
    public List<DailyLogMediaRequest>? Media { get; set; }
}
