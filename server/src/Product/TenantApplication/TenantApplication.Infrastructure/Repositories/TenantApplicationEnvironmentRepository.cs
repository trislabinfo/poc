using Microsoft.EntityFrameworkCore;
using TenantApplication.Domain.Entities;
using TenantApplication.Domain.Enums;
using TenantApplication.Domain.Repositories;
using TenantApplication.Infrastructure.Data;

namespace TenantApplication.Infrastructure.Repositories;

public sealed class TenantApplicationEnvironmentRepository : ITenantApplicationEnvironmentRepository
{
    private readonly TenantApplicationDbContext _context;

    public TenantApplicationEnvironmentRepository(TenantApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantApplicationEnvironment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplicationEnvironments
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<TenantApplicationEnvironment?> GetByTenantAppAndEnvironmentAsync(Guid tenantApplicationId, EnvironmentType environmentType, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplicationEnvironments
            .FirstOrDefaultAsync(e => e.TenantApplicationId == tenantApplicationId && e.EnvironmentType == environmentType, cancellationToken);
    }

    public async Task<List<TenantApplicationEnvironment>> GetByTenantApplicationAsync(Guid tenantApplicationId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplicationEnvironments
            .Where(e => e.TenantApplicationId == tenantApplicationId)
            .OrderBy(e => e.EnvironmentType)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(TenantApplicationEnvironment env, CancellationToken cancellationToken = default)
    {
        await _context.TenantApplicationEnvironments.AddAsync(env, cancellationToken);
    }

    public void Update(TenantApplicationEnvironment env)
    {
        _context.TenantApplicationEnvironments.Update(env);
    }
}
