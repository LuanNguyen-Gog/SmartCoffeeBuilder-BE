namespace SmartCoffeeBuilder.Service.DTOs.Responses.Post;

public class PostResponse
{
    public Guid Id { get; set; }
    public Guid ProjectShopOwnerId { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectAddress { get; set; }

    /// <summary>
    /// Toạ độ mặt bằng, để marketplace vẽ được pin / ảnh bản đồ mà không phải geocode lại từ
    /// chuỗi địa chỉ ở mỗi thẻ. <c>null</c> khi chủ quán chưa ghim bản đồ.
    /// </summary>
    public double? ProjectLatitude { get; set; }

    /// <inheritdoc cref="ProjectLatitude"/>
    public double? ProjectLongitude { get; set; }

    public decimal? ProjectBudget { get; set; }
    public decimal? ProjectAreaM2 { get; set; }
    public string ServiceKind { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? SubmissionDeadline { get; set; }

    /// <summary>true khi bài đang trong thời gian đẩy nổi bật (đã trả phí).</summary>
    public bool IsBoosted { get; set; }
    public DateTime? BoostedUntil { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static PostResponse From(SmartCoffeeBuilder.Repository.Models.Post p) => new()
    {
        Id = p.Id,
        ProjectShopOwnerId = p.ProjectShopOwnerId,
        ProjectName = p.ProjectShopOwner?.Name,
        ProjectAddress = p.ProjectShopOwner?.Address,
        ProjectLatitude = p.ProjectShopOwner?.Latitude,
        ProjectLongitude = p.ProjectShopOwner?.Longitude,
        ProjectBudget = p.ProjectShopOwner?.Budget,
        ProjectAreaM2 = p.ProjectShopOwner?.AreaM2,
        ServiceKind = p.ServiceKind.ToString(),
        Title = p.Title,
        Description = p.Description,
        Status = p.Status.ToString(),
        SubmissionDeadline = p.SubmissionDeadline,
        IsBoosted = p.BoostedUntil.HasValue && p.BoostedUntil.Value > DateTime.UtcNow,
        BoostedUntil = p.BoostedUntil,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
