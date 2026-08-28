using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Apply;

/// <summary>Chỉ sửa được khi application còn pending.</summary>
public class UpdateApplyRequest
{
    public string? Proposal { get; set; }

    [Range(1, int.MaxValue)]
    public int? EstimatedDurationDays { get; set; }

    /// <summary>
    /// Xoá hẳn thời lượng đã điền (về null). Cần cờ riêng vì đây là partial update:
    /// <see cref="EstimatedDurationDays"/> = null nghĩa là "đừng đụng tới", nên không có
    /// cờ này thì con số đã lỡ điền không gỡ ra được — trong khi thời lượng ở bước ứng
    /// tuyển vốn là tuỳ chọn (provider có thể chưa đủ dữ kiện để ước lượng).
    /// Gửi kèm một giá trị cho <see cref="EstimatedDurationDays"/> là mâu thuẫn ⇒ 400.
    /// </summary>
    public bool ClearEstimatedDuration { get; set; }
}
