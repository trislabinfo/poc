using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Enums;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using System.Text.Json;
using TenantApplication.Application.Services;
using TenantApplication.Infrastructure.Data;

namespace TenantApplication.Infrastructure.Services;

/// <summary>
/// Copies entity, property, and relation definitions from a platform release's EntityJson
/// into the tenant application's definition tables, remapping IDs so the tenant can generate DDL.
/// </summary>
public sealed class CopyDefinitionsFromPlatformReleaseService : ICopyDefinitionsFromPlatformReleaseService
{
    private readonly TenantApplicationDbContext _context;
    private readonly IPlatformReleaseSnapshotProvider _snapshotProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    // Platform release EntityJson is serialized with default (PascalCase) from AppBuilder.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CopyDefinitionsFromPlatformReleaseService(
        TenantApplicationDbContext context,
        IPlatformReleaseSnapshotProvider snapshotProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _snapshotProvider = snapshotProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<Result> CopyAsync(
        Guid tenantApplicationId,
        Guid platformReleaseId,
        CancellationToken cancellationToken = default)
    {
        var snapshotDto = await _snapshotProvider.GetSnapshotAsync(platformReleaseId, cancellationToken);
        if (snapshotDto == null)
            return Result.Failure(Error.NotFound("TenantApplication.PlatformReleaseNotFound", "Platform release not found."));

        // 1) Copy navigation definitions
        if (!string.IsNullOrWhiteSpace(snapshotDto.NavigationJson) && snapshotDto.NavigationJson != "[]")
        {
            List<NavigationDataDto>? navList;
            try
            {
                navList = JsonSerializer.Deserialize<List<NavigationDataDto>>(snapshotDto.NavigationJson, JsonOptions);
            }
            catch (JsonException ex)
            {
                return Result.Failure(Error.Validation("TenantApplication.InvalidNavigationSnapshot", $"Invalid navigation snapshot JSON: {ex.Message}"));
            }
            if (navList != null)
            {
                foreach (var nav in navList)
                {
                    var navResult = NavigationDefinition.Create(
                        tenantApplicationId,
                        nav.Name ?? "Navigation",
                        nav.ConfigurationJson ?? "{}",
                        _dateTimeProvider);
                    if (navResult.IsFailure)
                        return Result.Failure(navResult.Error);
                    _context.NavigationDefinitions.Add(navResult.Value);
                }
            }
        }

        var entityJson = snapshotDto.EntityJson;
        if (string.IsNullOrWhiteSpace(entityJson) || entityJson == "{}")
            return Result.Success(); // No entities to copy is OK

        PlatformReleaseEntitySnapshotDto? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<PlatformReleaseEntitySnapshotDto>(entityJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            return Result.Failure(Error.Validation("TenantApplication.InvalidSnapshot", $"Invalid entity snapshot JSON: {ex.Message}"));
        }

        if (snapshot?.Entities == null || snapshot.Entities.Count == 0)
            return Result.Success();

        var entityIdMap = new Dictionary<Guid, Guid>(); // old platform entity Id -> new tenant entity Id

        // 2) Create entity definitions
        foreach (var item in snapshot.Entities)
        {
            if (item.Entity == null) continue;
            var e = item.Entity;
            var entityResult = EntityDefinition.Create(
                tenantApplicationId,
                e.Name ?? "Entity",
                e.DisplayName ?? e.Name ?? "Entity",
                _dateTimeProvider,
                e.Description,
                e.AttributesJson ?? "{}",
                e.PrimaryKey);
            if (entityResult.IsFailure)
                return Result.Failure(entityResult.Error);
            var newEntity = entityResult.Value;
            entityIdMap[e.Id] = newEntity.Id;
            _context.EntityDefinitions.Add(newEntity);
        }

        // 3) Create property definitions (need new entity IDs)
        foreach (var item in snapshot.Entities)
        {
            if (item.Entity == null || item.Properties == null) continue;
            var newEntityId = entityIdMap.GetValueOrDefault(item.Entity.Id);
            if (newEntityId == Guid.Empty) continue;
            var order = 0;
            foreach (var p in item.Properties)
            {
                var propResult = PropertyDefinition.Create(
                    newEntityId,
                    p.Name ?? "Property",
                    p.DisplayName ?? p.Name ?? "Property",
                    (PropertyDataType)(p.DataType),
                    p.IsRequired,
                    order++,
                    _dateTimeProvider);
                if (propResult.IsFailure)
                    return Result.Failure(propResult.Error);
                _context.PropertyDefinitions.Add(propResult.Value);
            }
        }

        // 4) Create relation definitions with mapped entity IDs
        if (snapshot.Relations != null)
        {
            foreach (var r in snapshot.Relations)
            {
                var newSourceId = entityIdMap.GetValueOrDefault(r.SourceEntityId);
                var newTargetId = entityIdMap.GetValueOrDefault(r.TargetEntityId);
                if (newSourceId == Guid.Empty || newTargetId == Guid.Empty)
                    continue;
                var relResult = RelationDefinition.Create(
                    newSourceId,
                    newTargetId,
                    r.Name ?? "Relation",
                    (RelationType)r.RelationType,
                    r.CascadeDelete,
                    _dateTimeProvider);
                if (relResult.IsFailure)
                    return Result.Failure(relResult.Error);
                _context.RelationDefinitions.Add(relResult.Value);
            }
        }

        // Caller (InstallApplicationCommandHandler) saves the unit of work once.
        return Result.Success();
    }

    /// <summary>DTO for deserializing platform release EntityJson (camelCase or PascalCase).</summary>
    private sealed class PlatformReleaseEntitySnapshotDto
    {
        public List<EntitySnapshotItemDto>? Entities { get; set; }
        public List<RelationSnapshotItemDto>? Relations { get; set; }
    }

    private sealed class EntitySnapshotItemDto
    {
        public EntityDataDto? Entity { get; set; }
        public List<PropertyDataDto>? Properties { get; set; }
    }

    private sealed class EntityDataDto
    {
        public Guid Id { get; set; }
        public Guid AppDefinitionId { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? AttributesJson { get; set; }
        public string? PrimaryKey { get; set; }
    }

    private sealed class PropertyDataDto
    {
        public int DataType { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public bool IsRequired { get; set; }
        public int Order { get; set; }
    }

    private sealed class RelationSnapshotItemDto
    {
        public Guid SourceEntityId { get; set; }
        public Guid TargetEntityId { get; set; }
        public string? Name { get; set; }
        public int RelationType { get; set; }
        public bool CascadeDelete { get; set; }
    }

    private sealed class NavigationDataDto
    {
        public string? Name { get; set; }
        public string? ConfigurationJson { get; set; }
    }
}
