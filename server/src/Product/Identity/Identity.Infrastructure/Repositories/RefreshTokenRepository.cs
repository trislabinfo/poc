using BuildingBlocks.Infrastructure.Persistence;
using BuildingBlocks.Kernel.Domain;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken, Guid>, IRefreshTokenRepository
{
    private readonly IdentityDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenRepository(IdentityDbContext context, IDateTimeProvider dateTimeProvider)
        : base(context)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            _ = token.Revoke(_dateTimeProvider);
        }
    }
}
