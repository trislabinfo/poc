using BuildingBlocks.Kernel.Persistence;

using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    #region Future Methods - Phase 2
    // TODO Phase 2: Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);
    // TODO Phase 2: Task<IEnumerable<User>> GetActiveUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    // TODO Phase 2: Task<IEnumerable<User>> GetLockedUsersAsync(Guid tenantId, CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<User>> GetUsersWithExpiredPasswordsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    #endregion
}

