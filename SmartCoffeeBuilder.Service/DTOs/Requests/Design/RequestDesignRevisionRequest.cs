using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Design;

public class RequestDesignRevisionRequest
{
    /// <summary>Lý do owner yêu cầu chỉnh sửa bản design.</summary>
    [Required]
    public string Reason { get; set; } = null!;

    /// <summary>
    /// Owner XÁC NHẬN chấp nhận phí sửa khi vòng này đã vượt số lần miễn phí cam kết trong báo giá
    /// (review 1.1). Bỏ trống mà vòng sửa đã vượt hạn mức thì API trả 409 kèm số tiền — để owner
    /// biết mình đang đồng ý trả bao nhiêu trước khi bấm lần nữa.
    ///
    /// Còn trong hạn mức thì cờ này vô hại: không có phí nào được lập.
    /// </summary>
    public bool AcceptExtraFee { get; set; }
}
