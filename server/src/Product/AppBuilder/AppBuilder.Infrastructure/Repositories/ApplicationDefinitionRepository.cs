using AppBuilder.Domain.Repositories;
using AppBuilder.Infrastructure.Data;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AppBuilder.Infrastructure.Repositories;

public class AppDefinitionRepository : Repository<AppDefinition.Domain.Entities.Application.AppDefinition, Guid>, IAppDefinitionRepository
{
    private readonly AppBuilderDbContext _context;

    public AppDefinitionRepository(AppBuilderDbContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<List<AppDefinition.Domain.Entities.Application.AppDefinition>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AppDefinitions.ToListAsync(cancellationToken);
    }

    public async Task<AppDefinition.Domain.Entities.Application.AppDefinition?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.AppDefinitions
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.AppDefinitions
            .AnyAsync(x => x.Slug == slug, cancellationToken);
    }
}
