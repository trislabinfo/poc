using AppDefinition.Domain.Enums;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppDefinition.Domain.Entities.Application;

/// <summary>Entity property/field definition (shared by AppBuilder and TenantApplication).</summary>
public sealed class PropertyDefinition : Entity<Guid>
{
    public Guid EntityDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public PropertyDataType DataType { get; private set; }
    public bool IsRequired { get; private set; }
    public string? DefaultValue { get; private set; }
    public string ValidationRulesJson { get; private set; } = "{}";
    public int Order { get; private set; }

    private PropertyDefinition() { }

    public static Result<PropertyDefinition> Create(
        Guid entityDefinitionId,
        string name,
        string displayName,
        PropertyDataType dataType,
        bool isRequired,
        int order,
        IDateTimeProvider dateTimeProvider)
    {
        if (entityDefinitionId == Guid.Empty)
            return Result<PropertyDefinition>.Failure(
                Error.Validation("AppDefinition.Property.EntityDefinitionId", "Entity definition ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<PropertyDefinition>.Failure(nameResult.Error);
        var displayResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        if (displayResult.IsFailure) return Result<PropertyDefinition>.Failure(displayResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<PropertyDefinition>.Failure(dateTimeProviderResult.Error);
        if (order < 0)
            return Result<PropertyDefinition>.Failure(
                Error.Validation("AppDefinition.Property.Order", "Order must be non-negative."));

        var now = dateTimeProvider.UtcNow;
        return Result<PropertyDefinition>.Success(new PropertyDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entityDefinitionId,
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            DataType = dataType,
            IsRequired = isRequired,
            ValidationRulesJson = "{}",
            Order = order,
            CreatedAt = now
        });
    }

    public Result Update(string displayName, bool isRequired, string? defaultValue, string? validationRulesJson, int order, IDateTimeProvider? dateTimeProvider = null)
    {
        var displayResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        if (displayResult.IsFailure) return displayResult;
        DisplayName = displayName.Trim();
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        ValidationRulesJson = validationRulesJson ?? "{}";
        Order = order;
        if (dateTimeProvider != null) UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }
}
