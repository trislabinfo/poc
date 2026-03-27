using BuildingBlocks.Kernel.Persistence;

using Identity.Domain.Entities;

namespace Identity.Domain.Repositories;

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> GetByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetAllAsync(Guid tenantId, CancellationToken cancellationToken = default);

    #region Future Methods - Phase 2
    // TODO Phase 2: Task<Role?> GetByIdWithPermissionsAsync(Guid id, CancellationToken cancellationToken = default);
    // TODO Phase 2: Task<IEnumerable<Role>> GetSystemRolesAsync(CancellationToken cancellationToken = default);
    // TODO Phase 3: Task<IEnumerable<Role>> GetRoleHierarchyAsync(Guid roleId, CancellationToken cancellationToken = default);
    #endregion
}

