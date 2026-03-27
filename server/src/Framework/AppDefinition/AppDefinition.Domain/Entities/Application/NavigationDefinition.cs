using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppDefinition.Domain.Entities.Application;

/// <summary>Navigation structure for an application (shared by AppBuilder and TenantApplication).</summary>
public sealed class NavigationDefinition : Entity<Guid>
{
    public Guid AppDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ConfigurationJson { get; private set; } = string.Empty;

    private NavigationDefinition() { }

    public static Result<NavigationDefinition> Create(
        Guid AppDefinitionId,
        string name,
        string configurationJson,
        IDateTimeProvider dateTimeProvider)
    {
        if (AppDefinitionId == Guid.Empty)
            return Result<NavigationDefinition>.Failure(
                Error.Validation("AppDefinition.Navigation.AppDefinitionId", "Application definition ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<NavigationDefinition>.Failure(nameResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<NavigationDefinition>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        return Result<NavigationDefinition>.Success(new NavigationDefinition
        {
            Id = Guid.NewGuid(),
            AppDefinitionId = AppDefinitionId,
            Name = name.Trim(),
            ConfigurationJson = configurationJson ?? "{}",
            CreatedAt = now
        });
    }

    public Result Update(string name, string configurationJson, IDateTimeProvider dateTimeProvider)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return nameResult;
        Name = name.Trim();
        ConfigurationJson = configurationJson ?? "{}";
        UpdatedAt = dateTimeProvider?.UtcNow ?? UpdatedAt;
        return Result.Success();
    }
}
