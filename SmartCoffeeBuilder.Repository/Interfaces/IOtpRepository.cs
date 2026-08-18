using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Repository.Interfaces;

public interface IOtpRepository
{
    /// <summary>Lấy OTP còn hiệu lực (chưa dùng, chưa hết hạn) của tài khoản.</summary>
    Task<Otp?> GetActiveAsync(Guid accountId);

    /// <summary>Lấy toàn bộ OTP còn hiệu lực — dùng cho job refresh của Hangfire.</summary>
    Task<ICollection<Otp>> GetAllActiveAsync();

    Task AddAsync(Otp otp);
    Task SaveChangesAsync();

    /// <summary>Xoá các OTP đã dùng hoặc đã hết hạn. Trả về số dòng đã xoá.</summary>
    Task<int> DeleteExpiredOrUsedAsync();
}
