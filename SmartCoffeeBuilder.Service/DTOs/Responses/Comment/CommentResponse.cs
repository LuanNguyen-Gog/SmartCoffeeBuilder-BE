using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Comment;

public class CommentResponse
{
    public long Id { get; set; }
    public string TargetType { get; set; } = null!;
    public long TargetId { get; set; }
    public string? Body { get; set; }
    public long? CreatedBy { get; set; }

    /// <summary>
    /// Tên hiển thị của người viết — join ngược từ Account tới ShopOwner.FullName (owner) hoặc
    /// ServiceProviderProfile.DisplayName (provider); admin thì lấy email. Để trống nếu không tìm thấy.
    /// </summary>
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static CommentResponse From(Comment c) => new()
    {
        Id = c.Id,
        TargetType = c.TargetType.ToString(),
        TargetId = c.TargetId,
        Body = c.Body,
        CreatedBy = c.CreatedBy,
        CreatedByName = ResolveDisplayName(c.CreatedByAccount),
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    private static string? ResolveDisplayName(Account? account)
    {
        if (account == null) return null;

        // Ưu tiên tên hiển thị từ profile tương ứng; cuối cùng mới rơi về email.
        if (account.ShopOwner != null) return account.ShopOwner.FullName;
        if (account.ServiceProviderProfile != null) return account.ServiceProviderProfile.DisplayName;
        return account.Email;
    }
}