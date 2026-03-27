using Microsoft.EntityFrameworkCore;
using TenantApplication.Domain.Repositories;
using TenantApplication.Infrastructure.Data;
using TenantApplicationEntity = TenantApplication.Domain.Entities.TenantApplication;

namespace TenantApplication.Infrastructure.Repositories;

public sealed class TenantApplicationRepository : ITenantApplicationRepository
{
    private readonly TenantApplicationDbContext _context;

    public TenantApplicationRepository(TenantApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TenantApplicationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplications
            .Include(a => a.Environments)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<TenantApplicationEntity?> GetBySlugAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplications
            .Include(a => a.Environments)
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Slug == slug, cancellationToken);
    }

    public async Task<List<TenantApplicationEntity>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplications
            .Include(a => a.Environments)
            .Where(a => a.TenantId == tenantId)
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantApplicationEntity?> GetByTenantAndApplicationAsync(Guid tenantId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplications
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.ApplicationId == applicationId, cancellationToken);
    }

    public async Task<bool> SlugExistsForTenantAsync(Guid tenantId, string slug, CancellationToken cancellationToken = default)
    {
        return await _context.TenantApplications
            .AnyAsync(a => a.TenantId == tenantId && a.Slug == slug, cancellationToken);
    }

    public async Task AddAsync(TenantApplicationEntity app, CancellationToken cancellationToken = default)
    {
        await _context.TenantApplications.AddAsync(app, cancellationToken);
    }

    public void Update(TenantApplicationEntity app)
    {
        _context.TenantApplications.Update(app);
    }

    public void Remove(TenantApplicationEntity app)
    {
        _context.TenantApplications.Remove(app);
    }
}
