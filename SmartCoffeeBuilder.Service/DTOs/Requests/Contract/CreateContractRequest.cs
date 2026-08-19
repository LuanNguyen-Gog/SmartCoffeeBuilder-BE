using System.ComponentModel.DataAnnotations;

namespace SmartCoffeeBuilder.Service.DTOs.Requests.Contract;

/// <summary>Provider tạo bản hợp đồng (draft) cho một engagement 'accepted'.</summary>
public class CreateContractRequest
{
    [Required]
    public Guid ProjectWorkingId { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    /// <summary>Thông tin các bên (JSON/text tự do).</summary>
    public string? PartyInfo { get; set; }

    /// <summary>Điều khoản hợp đồng.</summary>
    public string? Terms { get; set; }

    /// <summary>
    /// Báo giá đã được owner duyệt để dựng hợp đồng này (review 3). Khi có giá trị thì
    /// <see cref="AgreedValue"/> bị BỎ QUA — giá trị hợp đồng lấy thẳng từ tổng báo giá.
    /// </summary>
    public Guid? QuotationId { get; set; }

    /// <summary>Giá trị thoả thuận (tham khảo). Bỏ qua khi hợp đồng dựng từ báo giá.</summary>
    public decimal? AgreedValue { get; set; }

    /// <summary>URL file hợp đồng đã upload.</summary>
    public string? DocumentUrl { get; set; }
}
