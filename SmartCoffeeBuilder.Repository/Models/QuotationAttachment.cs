namespace SmartCoffeeBuilder.Repository.Models;

/// <summary>
/// File đính kèm báo giá (review 3: "Các file đính kèm theo nếu có").
/// Lưu ObjectName trên bucket GCS như mọi chỗ khác trong hệ thống — xem IFileStorageService.
/// </summary>
public class QuotationAttachment
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }

    /// <summary>ObjectName trên bucket (FE hiển thị bằng URL public do BE resolve).</summary>
    public string FileUrl { get; set; } = null!;

    public string? FileName { get; set; }
    public Guid? UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }

    public Quotation Quotation { get; set; } = null!;
    public Account? UploadedByAccount { get; set; }
}
