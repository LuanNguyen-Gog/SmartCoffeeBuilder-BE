using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.DBContext;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Repository.Implementations;

public class OtpRepository : IOtpRepository
{
    private readonly SmartCafeBuilderContext _context;

    public OtpRepository(SmartCafeBuilderContext context)
    {
        _context = context;
    }

    public async Task<Otp?> GetActiveAsync(Guid accountId)
        => await _context.Otps
            .Where(o => o.AccountId == accountId
                        && !o.IsUsed
                        && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<ICollection<Otp>> GetAllActiveAsync()
        => await _context.Otps
            .Where(o => !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

    public async Task AddAsync(Otp otp)
    {
        _context.Otps.Add(otp);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async Task<int> DeleteExpiredOrUsedAsync()
        => await _context.Otps
            .Where(o => o.IsUsed || o.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync();
}
