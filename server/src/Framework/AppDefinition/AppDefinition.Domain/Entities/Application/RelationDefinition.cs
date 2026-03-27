using AppDefinition.Domain.Enums;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppDefinition.Domain.Entities.Application;

/// <summary>Entity relationship definition (shared by AppBuilder and TenantApplication).</summary>
public sealed class RelationDefinition : Entity<Guid>
{
    public Guid SourceEntityId { get; private set; }
    public Guid TargetEntityId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public RelationType RelationType { get; private set; }
    public bool CascadeDelete { get; private set; }

    private RelationDefinition() { }

    public static Result<RelationDefinition> Create(
        Guid sourceEntityId,
        Guid targetEntityId,
        string name,
        RelationType relationType,
        bool cascadeDelete,
        IDateTimeProvider dateTimeProvider)
    {
        if (sourceEntityId == Guid.Empty)
            return Result<RelationDefinition>.Failure(
                Error.Validation("AppDefinition.Relation.SourceEntityId", "Source entity ID is required."));
        if (targetEntityId == Guid.Empty)
            return Result<RelationDefinition>.Failure(
                Error.Validation("AppDefinition.Relation.TargetEntityId", "Target entity ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<RelationDefinition>.Failure(nameResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<RelationDefinition>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        return Result<RelationDefinition>.Success(new RelationDefinition
        {
            Id = Guid.NewGuid(),
            SourceEntityId = sourceEntityId,
            TargetEntityId = targetEntityId,
            Name = name.Trim(),
            RelationType = relationType,
            CascadeDelete = cascadeDelete,
            CreatedAt = now
        });
    }

    public void UpdateCascadeDelete(bool cascadeDelete)
    {
        CascadeDelete = cascadeDelete;
    }
}
