using SmartCoffeeBuilder.Repository.Models;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.ProjectShopOwner;

public class ProjectShopOwnerResponse
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public string Name { get; set; } = null!;
    public string Address { get; set; } = null!;
    public decimal AreaM2 { get; set; }
    public decimal Budget { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ProjectWorkingSummary> Providers { get; set; } = new();
    public ShopOwnerSummary? Owner { get; set; }

    /// <summary>
    /// Các post của project đang ở trạng thái <c>open</c> — provider có thể gửi application vào.
    /// </summary>
    public List<ProjectOpenPostSummary> OpenPosts { get; set; } = new();

    /// <summary>
    /// Các <c>service_kind</c> mà project đang mở cho provider apply (post ở trạng thái <c>open</c>
    /// và còn hạn nộp hồ sơ). Mỗi phần tử là một trong: <c>design</c>, <c>construction</c>, <c>both</c>.
    /// Rỗng nếu project không nhận application. FE có thể check <c>includes("design")</c>.
    /// </summary>
    public List<string> OpenFor { get; set; } = new();

    public static ProjectShopOwnerResponse From(SmartCoffeeBuilder.Repository.Models.ProjectShopOwner p)
    {
        var now = DateTime.UtcNow;
        var openPosts = p.Posts?
            .Where(post => post.Status == PostStatus.open)
            .Select(ProjectOpenPostSummary.From)
            .ToList() ?? new();

        var activeKinds = (p.Posts?
            .Where(post => post.Status == PostStatus.open &&
                           (post.SubmissionDeadline == null || post.SubmissionDeadline > now))
            .Select(post => post.ServiceKind)
            .ToHashSet() ?? new HashSet<ServiceKind>());

        var openFor = new List<string>();
        if (activeKinds.Contains(ServiceKind.design)) openFor.Add("design");
        if (activeKinds.Contains(ServiceKind.construction)) openFor.Add("construction");
        if (activeKinds.Contains(ServiceKind.both)) openFor.Add("both");

        return new ProjectShopOwnerResponse
        {
            Id = p.Id,
            OwnerId = p.OwnerId,
            Name = p.Name,
            Address = p.Address,
            AreaM2 = p.AreaM2,
            Budget = p.Budget,
            Status = p.Status.ToString(),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Providers = p.ProjectWorkings?
                .Select(ProjectWorkingSummary.From)
                .ToList() ?? new(),
            Owner = p.Owner != null ? ShopOwnerSummary.From(p.Owner) : null,
            OpenPosts = openPosts,
            OpenFor = openFor
        };
    }
}
