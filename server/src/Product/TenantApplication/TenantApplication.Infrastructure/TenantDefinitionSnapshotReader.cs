using AppDefinition.Domain.Entities.Application;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TenantApplication.Domain.Repositories;
using TenantApplication.Infrastructure.Data;

namespace TenantApplication.Infrastructure;

public sealed class TenantDefinitionSnapshotReader : ITenantDefinitionSnapshotReader
{
    private readonly TenantApplicationDbContext _context;

    public TenantDefinitionSnapshotReader(TenantApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(string NavigationJson, string PageJson, string DataSourceJson, string EntityJson)> GetSnapshotAsync(
        Guid tenantApplicationId,
        CancellationToken cancellationToken = default)
    {
        var navs = await _context.NavigationDefinitions
            .Where(x => x.AppDefinitionId == tenantApplicationId)
            .ToListAsync(cancellationToken);
        var pages = await _context.PageDefinitions
            .Where(x => x.AppDefinitionId == tenantApplicationId)
            .ToListAsync(cancellationToken);
        var dataSources = await _context.DataSourceDefinitions
            .Where(x => x.AppDefinitionId == tenantApplicationId)
            .ToListAsync(cancellationToken);
        var entities = await _context.EntityDefinitions
            .Where(x => x.AppDefinitionId == tenantApplicationId)
            .ToListAsync(cancellationToken);
        var relations = new List<object>();
        foreach (var e in entities)
        {
            var rels = await _context.RelationDefinitions
                .Where(r => r.SourceEntityId == e.Id)
                .ToListAsync(cancellationToken);
            relations.AddRange(rels);
        }
        var entityWithProps = new List<object>();
        foreach (var e in entities)
        {
            var props = await _context.PropertyDefinitions
                .Where(p => p.EntityDefinitionId == e.Id)
                .ToListAsync(cancellationToken);
            entityWithProps.Add(new { Entity = e, Properties = props });
        }
        var navigationJson = JsonSerializer.Serialize(navs);
        var pageJson = JsonSerializer.Serialize(pages);
        var dataSourceJson = JsonSerializer.Serialize(dataSources);
        var entityJson = JsonSerializer.Serialize(new { Entities = entityWithProps, Relations = relations });
        return (navigationJson, pageJson, dataSourceJson, entityJson);
    }

    public async Task<(IReadOnlyList<EntityDefinition> Entities, Dictionary<Guid, List<PropertyDefinition>> PropertiesByEntityId, IReadOnlyList<RelationDefinition> Relations)> GetSchemaDataAsync(
        Guid tenantApplicationId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _context.EntityDefinitions
            .Where(x => x.AppDefinitionId == tenantApplicationId)
            .ToListAsync(cancellationToken);

        var relations = new List<RelationDefinition>();
        foreach (var e in entities)
        {
            var rels = await _context.RelationDefinitions
                .Where(r => r.SourceEntityId == e.Id)
                .ToListAsync(cancellationToken);
            relations.AddRange(rels);
        }

        var propertiesByEntityId = new Dictionary<Guid, List<PropertyDefinition>>();
        foreach (var e in entities)
        {
            var props = await _context.PropertyDefinitions
                .Where(p => p.EntityDefinitionId == e.Id)
                .ToListAsync(cancellationToken);
            propertiesByEntityId[e.Id] = props;
        }

        return (entities, propertiesByEntityId, relations);
    }
}
