using BuildingBlocks.Kernel.Persistence;

using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IPermissionRepository : IRepository<Permission, Guid>
{
    Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    #region Future Methods - Phase 2
    // TODO Phase 2: Task<IEnumerable<Permission>> GetByModuleAsync(string module, CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<Permission>> GetPermissionTreeAsync(CancellationToken cancellationToken = default);
    #endregion
}

