using SmartCoffeeBuilder.Repository.Interfaces;
using AccountModel = SmartCoffeeBuilder.Repository.Models.Account;
using ShopOwnerModel = SmartCoffeeBuilder.Repository.Models.ShopOwner;
using ProviderModel = SmartCoffeeBuilder.Repository.Models.ServiceProviderProfile;
using SmartCoffeeBuilder.Repository.Models.Enums;

namespace SmartCoffeeBuilder.Service.DTOs.Responses.Chat;

/// <summary>
/// Thông tin hiển thị của người gửi — FE render avatar + tên trong message.
/// </summary>
/// <remarks>
/// Service tự build từ Account + ShopOwner / ServiceProviderProfile (không include sẵn
/// trong EF query để tránh join lớn — chỉ khi cần hiển thị mới lookup). Pattern giống
/// <c>MeResponse</c>: thông tin profile lồng vào response account.
/// </remarks>
public class SenderInfo
{
    public long AccountId { get; set; }

    /// <summary>Tên hiển thị — ShopOwner.FullName với role=owner, ServiceProviderProfile.DisplayName với role=provider.</summary>
    public string? DisplayName { get; set; }

    /// <summary>"owner" | "provider" | "admin".</summary>
    public string Role { get; set; } = null!;

    /// <summary>URL public avatar — null ở v1, bổ sung khi có bucket avatar.</summary>
    public string? AvatarUrl { get; set; }
}

public static class SenderInfoFactory
{
    /// <summary>
    /// Build SenderInfo từ Account + repo để lookup profile theo role. Profile để trống
    /// thì rơi về email. Service gọi đồng bộ trong các DTO From(...) của message/thread.
    /// </summary>
    public static async Task<SenderInfo> BuildAsync(
        AccountModel account,
        IGenericRepository<ShopOwnerModel> shopOwners,
        IGenericRepository<ProviderModel> providers)
    {
        string? displayName;
        switch (account.Role)
        {
            case AccountRole.owner:
                displayName = (await shopOwners
                    .SingleOrDefaultAsync(predicate: s => s.AccountId == account.Id))?.FullName;
                break;
            case AccountRole.provider:
                displayName = (await providers
                    .SingleOrDefaultAsync(predicate: p => p.AccountId == account.Id))?.DisplayName;
                break;
            default:
                displayName = null;
                break;
        }

        return new SenderInfo
        {
            AccountId = account.Id,
            DisplayName = displayName ?? account.Email,
            Role = account.Role.ToString(),
            AvatarUrl = null
        };
    }
}
