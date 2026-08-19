namespace SmartCoffeeBuilder.Service.DTOs.Requests.Quotation;

/// <summary>
/// Đính kèm file đã upload qua api/files vào báo giá: gửi ObjectName (hoặc URL public của bucket),
/// service tự chuẩn hoá về ObjectName như các entity khác.
/// </summary>
public class AddQuotationAttachmentRequest
{
    public string FileUrl { get; set; } = null!;
    public string? FileName { get; set; }
}
