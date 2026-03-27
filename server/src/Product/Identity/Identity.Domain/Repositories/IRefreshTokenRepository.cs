using BuildingBlocks.Kernel.Persistence;

using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid>
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    #region Future Methods - Phase 3
    // TODO Phase 3: Task<IEnumerable<RefreshToken>> GetActiveTokensForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<RefreshToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<RefreshToken>> GetTokenFamilyAsync(Guid tokenId, CancellationToken cancellationToken = default);
    #endregion
}

