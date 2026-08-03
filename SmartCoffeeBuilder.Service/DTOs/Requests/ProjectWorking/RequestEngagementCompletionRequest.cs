using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;

/// <summary>
/// Provider (designer/constructor) báo đã xong phần việc và xin owner nghiệm thu.
/// Không đổi provider_status — chỉ đặt mốc completion_requested_at.
/// </summary>
public class RequestEngagementCompletionRequest
{
    /// <summary>Ghi chú bàn giao gửi kèm cho owner (tuỳ chọn).</summary>
    [MaxLength(1000)]
    public string? Note { get; set; }
}
