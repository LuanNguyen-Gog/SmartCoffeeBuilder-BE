using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Repository.Models;

public class ProjectShopOwner
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;

    /// <summary>
    /// Vị trí mặt bằng trên bản đồ, kèm theo <see cref="Address"/> chứ không thay thế nó.
    ///
    /// Nullable vì hai lý do: mọi dự án tạo trước tính năng bản đồ đều không có toạ độ, và người
    /// dùng vẫn được phép chỉ gõ địa chỉ chữ mà không ghim bản đồ. Đã ghim thì PHẢI có đủ cả hai
    /// — xem <c>Service/Utils/GeoCoordinates.cs</c>.
    ///
    /// Lưu toạ độ thay vì geocode lại mỗi lần hiển thị: gọi lại API vừa tốn quota vừa có thể ra
    /// một điểm khác với điểm chủ quán đã tự tay ghim.
    /// </summary>
    public double? Latitude { get; set; }

    /// <inheritdoc cref="Latitude"/>
    public double? Longitude { get; set; }

    public decimal AreaM2 { get; set; }
    public decimal Budget { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.briefed;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ShopOwner Owner { get; set; } = null!;
    public DesignBrief? DesignBrief { get; set; }

    /// <summary>Hồ sơ thông số vật lý của mặt bằng (1-1) — kích thước, hướng, tầng, cửa/ban công.</summary>
    public SiteProfile? SiteProfile { get; set; }
    public ICollection<BudgetItem> BudgetItems { get; set; } = new List<BudgetItem>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<ProjectWorking> ProjectWorkings { get; set; } = new List<ProjectWorking>();
}
