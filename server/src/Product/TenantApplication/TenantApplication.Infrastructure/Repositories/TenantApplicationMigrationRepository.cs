using Microsoft.EntityFrameworkCore;
using TenantApplication.Domain.Entities;
using TenantApplication.Domain.Enums;
using TenantApplication.Domain.Repositories;
using TenantApplication.Infrastructure.Data;

namespace TenantApplication.Infrastructure.Repositories;

public sealed class TenantApplicationMigrationRepository : ITenantApplicationMigrationRepository
{
    private readonly TenantApplicationDbContext _context;

    public TenantApplicationMigrationRepository(TenantApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantApplicationMigration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplicationMigrations
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<List<TenantApplicationMigration>> GetByEnvironmentAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplicationMigrations
            .Where(m => m.TenantApplicationEnvironmentId == environmentId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantApplicationMigration?> GetPendingMigrationAsync(Guid environmentId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplicationMigrations
            .FirstOrDefaultAsync(m => m.TenantApplicationEnvironmentId == environmentId && m.Status == MigrationStatus.Pending, cancellationToken);
    }

    public async Task AddAsync(TenantApplicationMigration migration, CancellationToken cancellationToken = default)
    {
        await _context.TenantApplicationMigrations.AddAsync(migration, cancellationToken);
    }

    public void Update(TenantApplicationMigration migration)
    {
        _context.TenantApplicationMigrations.Update(migration);
    }
}
