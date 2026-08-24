using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class Design
{
    public Guid Id { get; set; }
    public Guid ProjectWorkingId { get; set; }
    public string? Title { get; set; }
    public decimal Version { get; set; } // decimal(4,1)
    public DesignType Type { get; set; }
    public string? Reason { get; set; }

    /// <summary>
    /// Provider mô tả ĐÃ ĐỔI GÌ so với bản trước (review 3, mục Designer). Điền lúc nộp bản mới;
    /// khác <see cref="Reason"/> (lý do owner trả bản về). Được đóng băng vào design_version để
    /// owner đọc lại lịch sử từng vòng.
    /// </summary>
    public string? ChangeSummary { get; set; }
    /// <summary>
    /// Số vòng owner ĐÃ yêu cầu sửa bản thiết kế này. Tăng 1 mỗi lần
    /// <c>DesignService.RequestRevisionAsync</c> chạy, KHÔNG bao giờ giảm.
    ///
    /// Đây là con số đối chiếu với <c>quotations.free_revision_count</c> để biết vòng sửa tiếp theo
    /// còn miễn phí hay đã phát sinh chi phí. Không suy từ <see cref="Version"/> được: version cũng
    /// tăng khi provider chủ động nộp lại, mà cái đó không phải owner đòi sửa.
    /// </summary>
    public int RevisionCount { get; set; }

    public DesignStatus Status { get; set; } = DesignStatus.in_progress;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ProjectWorking ProjectWorking { get; set; } = null!;
    public Account? CreatedByAccount { get; set; }
    public ICollection<DesignImage> DesignImages { get; set; } = new List<DesignImage>();

    /// <summary>Checklist nghiệm thu bản thiết kế này (review 3).</summary>
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();

    /// <summary>Các khoản phát sinh chi phí do vòng sửa vượt hạn mức của bản thiết kế này.</summary>
    public ICollection<ChangeOrder> ChangeOrders { get; set; } = new List<ChangeOrder>();
}
