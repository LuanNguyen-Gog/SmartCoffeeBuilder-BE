using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;

/// <summary>
/// Một bên (owner hoặc provider) đề nghị huỷ ngang hợp tác đang chạy.
/// KHÔNG đổi provider_status — engagement vẫn 'accepted' cho tới khi bên kia đồng ý.
/// </summary>
public class RequestEngagementTerminationRequest
{
    /// <summary>Lý do huỷ ngang gửi cho bên kia (tuỳ chọn nhưng nên có).</summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }
}
