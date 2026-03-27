using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppDefinition.Domain.Entities.Application;

/// <summary>
/// Database entity/table definition (shared by AppBuilder and TenantApplication).
/// AppDefinitionId = parent app id (AppBuilder: application definition; TenantApplication: tenant application id).
/// </summary>
public sealed class EntityDefinition : Entity<Guid>
{
    public Guid AppDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string AttributesJson { get; private set; } = "{}";
    public string? PrimaryKey { get; private set; }

    private EntityDefinition() { }

    public static Result<EntityDefinition> Create(
        Guid AppDefinitionId,
        string name,
        string displayName,
        IDateTimeProvider dateTimeProvider,
        string? description = null,
        string? attributesJson = null,
        string? primaryKey = null)
    {
        if (AppDefinitionId == Guid.Empty)
            return Result<EntityDefinition>.Failure(
                Error.Validation("AppDefinition.Entity.AppDefinitionId", "Application definition ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<EntityDefinition>.Failure(nameResult.Error);
        var displayResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        if (displayResult.IsFailure) return Result<EntityDefinition>.Failure(displayResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<EntityDefinition>.Failure(dateTimeProviderResult.Error);
        if (name.Trim().Length > 100)
            return Result<EntityDefinition>.Failure(
                Error.Validation("AppDefinition.Entity.NameTooLong", "Name cannot exceed 100 characters."));
        if (displayName.Trim().Length > 200)
            return Result<EntityDefinition>.Failure(
                Error.Validation("AppDefinition.Entity.DisplayNameTooLong", "Display name cannot exceed 200 characters."));
        if (description != null && description.Length > 500)
            return Result<EntityDefinition>.Failure(
                Error.Validation("AppDefinition.Entity.DescriptionTooLong", "Description cannot exceed 500 characters."));

        var now = dateTimeProvider.UtcNow;
        return Result<EntityDefinition>.Success(new EntityDefinition
        {
            Id = Guid.NewGuid(),
            AppDefinitionId = AppDefinitionId,
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            AttributesJson = attributesJson ?? "{}",
            PrimaryKey = primaryKey?.Trim(),
            CreatedAt = now
        });
    }

    public Result Update(string name, string displayName, string? description = null, string? attributesJson = null, string? primaryKey = null, IDateTimeProvider? dateTimeProvider = null)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return nameResult;
        var displayResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        if (displayResult.IsFailure) return displayResult;
        if (description != null && description.Length > 500)
            return Result.Failure(Error.Validation("AppDefinition.Entity.DescriptionTooLong", "Description cannot exceed 500 characters."));
        Name = name.Trim();
        DisplayName = displayName.Trim();
        Description = description?.Trim();
        AttributesJson = attributesJson ?? AttributesJson;
        PrimaryKey = primaryKey?.Trim();
        if (dateTimeProvider != null) UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }
}
