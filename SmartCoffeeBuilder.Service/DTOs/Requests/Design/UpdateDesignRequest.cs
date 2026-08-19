namespace SmartCoffeeBuilder.Service.DTOs.Requests.Design;

public class UpdateDesignRequest
{
    public string? Title { get; set; }

    /// <summary>concept | layout_2d | render_3d | technical_drawing</summary>
    public string? Type { get; set; }

    /// <summary>
    /// Mô tả đã thay đổi gì so với bản trước (review 3). Điền trước khi submit để owner đọc kèm
    /// bản mới; giá trị được đóng băng vào snapshot design_version.
    /// </summary>
    public string? ChangeSummary { get; set; }
}
