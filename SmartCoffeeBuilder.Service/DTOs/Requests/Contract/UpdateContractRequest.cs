namespace SmartCoffeeBuilder.Service.DTOs.Requests.Contract;

/// <summary>Cập nhật nội dung hợp đồng — chỉ khi còn 'drafted'. Field null = giữ nguyên.</summary>
public class UpdateContractRequest
{
    public string? Title { get; set; }
    public string? PartyInfo { get; set; }
    public string? Terms { get; set; }
    public decimal? AgreedValue { get; set; }
    public string? DocumentUrl { get; set; }
}
