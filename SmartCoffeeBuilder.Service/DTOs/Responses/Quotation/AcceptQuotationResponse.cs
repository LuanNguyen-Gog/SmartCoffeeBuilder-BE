using SmartCoffeeBuilder.Service.DTOs.Responses.ProjectWorking;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Quotation;

/// <summary>
/// Kết quả owner duyệt báo giá. Với báo giá đi kèm hồ sơ ứng tuyển, duyệt báo giá ĐỒNG THỜI là
/// chọn provider: hệ thống chấp nhận luôn hồ sơ đó và mở engagement (<see cref="Engagement"/>),
/// các hồ sơ + báo giá còn lại của bài đăng bị đóng. Với báo giá của đường mời trực tiếp thì
/// engagement đã có sẵn nên trường này null.
/// </summary>
public class AcceptQuotationResponse
{
    public QuotationResponse Quotation { get; set; } = null!;
    public ProjectWorkingResponse? Engagement { get; set; }
}
