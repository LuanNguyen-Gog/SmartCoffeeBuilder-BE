namespace SmartCoffeeBuilder.Service.DTOs.Requests.ProjectShopOwner;

public class UpdateProjectShopOwnerRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// Toạ độ mới khi chủ quán ghim lại vị trí. Gửi cả hai để cập nhật; không gửi thì giữ nguyên
    /// toạ độ cũ. Muốn GỠ ghim (quay về địa chỉ chữ) thì gửi <see cref="ClearCoordinates"/> —
    /// riêng ở đây <c>null</c> nghĩa là "không đụng tới", không phải "xoá đi".
    /// </summary>
    public double? Latitude { get; set; }

    /// <inheritdoc cref="Latitude"/>
    public double? Longitude { get; set; }

    /// <summary>
    /// Gỡ ghim bản đồ, đưa dự án về chỉ còn địa chỉ chữ.
    ///
    /// Cần cờ riêng vì PATCH kiểu này dùng <c>null</c> để nói "bỏ qua trường này". Không có cờ thì
    /// không cách nào diễn đạt "xoá toạ độ" — gửi <c>null</c> chỉ khiến toạ độ cũ nằm nguyên đó.
    /// </summary>
    public bool ClearCoordinates { get; set; }

    public decimal? AreaM2 { get; set; }
    public decimal? Budget { get; set; }

    /// <summary>briefed | in_progress | completed | cancelled</summary>
    public string? Status { get; set; }
}
