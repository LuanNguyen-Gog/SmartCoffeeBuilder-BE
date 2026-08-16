using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Comment;

// Lưu ý: tên folder "Comment" làm cho `Comment` ở đây trở thành namespace con (CS0118),
// che mất class SmartCoffeeBuilder.Repository.Models.Comment. Phải fully qualify.
using CommentEntity = SmartCoffeeBuilder.Repository.Models.Comment;
using AccountEntity = SmartCoffeeBuilder.Repository.Models.Account;

public class CommentResponse
{
    public Guid Id { get; set; }
    public string TargetType { get; set; } = null!;
    public Guid TargetId { get; set; }
    public string? Body { get; set; }
    public Guid? CreatedBy { get; set; }

    /// <summary>
    /// Tên hiển thị của người viết — join ngược từ Account tới ShopOwner.FullName (owner) hoặc
    /// ServiceProviderProfile.DisplayName (provider); admin thì lấy email. Để trống nếu không tìm thấy.
    /// </summary>
    public string? CreatedByName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static CommentResponse From(CommentEntity c) => new()
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

    private static string? ResolveDisplayName(AccountEntity? account)
    {
        if (account == null) return null;

        // Ưu tiên tên hiển thị từ profile tương ứng; cuối cùng mới rơi về email.
        if (account.ShopOwner != null) return account.ShopOwner.FullName;
        if (account.ServiceProviderProfile != null) return account.ServiceProviderProfile.DisplayName;
        return account.Email;
    }
}