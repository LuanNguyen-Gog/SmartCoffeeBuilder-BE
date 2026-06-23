using Microsoft.EntityFrameworkCore;
using SmartCoffeeBuilder.Repository.Interfaces;
using SmartCoffeeBuilder.Repository.Models;

namespace SmartCoffeeBuilder.Repository.Implementations;

public class AuthRepository : IAuthRepository
{
    private readonly SmartCafeBuilderContext _context;

    public AuthRepository(SmartCafeBuilderContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByEmailAsync(string email)
        => await _context.Accounts
            .FirstOrDefaultAsync(a => a.Email == email && a.DeletedAt == null);

    public async Task<Account> CreateAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        => await _context.RefreshTokens
            .Include(rt => rt.Account)
            .FirstOrDefaultAsync(rt => rt.Token == token);

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken)
    {
        refreshToken.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllAccountRefreshTokensAsync(long accountId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.AccountId == accountId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
