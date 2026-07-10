using Microsoft.EntityFrameworkCore;
using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Persistence.Context;

namespace SigortaPro.Persistence.Identity;

// ADR-014: Refresh token'lar Persistence'ta yönetilir.
public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly AppDbContext _context;

    public RefreshTokenService(AppDbContext context)
    {
        _context = context;
    }

    public async Task StoreAsync(Guid userId, string token, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Guid?> GetActiveUserIdAsync(string token, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);

        if (entity is null || !entity.IsActive(DateTime.UtcNow))
        {
            return null;
        }

        return entity.UserId;
    }

    public async Task RevokeAsync(string token, string? replacedByToken = null, CancellationToken cancellationToken = default)
    {
        var entity = await _context.RefreshTokens
            .FirstOrDefaultAsync(refreshToken => refreshToken.Token == token, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.RevokedAt = DateTime.UtcNow;
        entity.ReplacedByToken = replacedByToken;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
