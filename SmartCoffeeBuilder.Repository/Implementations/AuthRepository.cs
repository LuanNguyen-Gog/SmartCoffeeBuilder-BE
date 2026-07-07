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

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email && u.DeletedAt == null);

    public async Task<Role?> GetRoleByNameAsync(string name)
        => await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);

    public async Task<User> CreateUserAsync(User user, long roleId)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        => await _context.RefreshTokens
            .Include(rt => rt.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
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

    public async Task RevokeAllUserRefreshTokensAsync(long userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
            t.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
