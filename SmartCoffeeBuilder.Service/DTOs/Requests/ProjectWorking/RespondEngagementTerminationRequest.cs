using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectWorking;

/// <summary>
/// Bên CÒN LẠI phản hồi đề nghị huỷ ngang: đồng ý (engagement → 'terminated')
/// hoặc từ chối (xoá đề nghị, engagement giữ 'accepted').
/// </summary>
public class RespondEngagementTerminationRequest
{
    /// <summary>true = đồng ý huỷ ngang; false = từ chối, hợp tác tiếp tục.</summary>
    [Required]
    public bool Approve { get; set; }

    /// <summary>Ghi chú phản hồi gửi lại bên đề nghị (tuỳ chọn).</summary>
    [MaxLength(1000)]
    public string? Note { get; set; }
}
