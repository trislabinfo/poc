using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppDefinition.Domain.Entities.Application;

/// <summary>Page layout and widgets for an application (shared by AppBuilder and TenantApplication).</summary>
public sealed class PageDefinition : Entity<Guid>
{
    public Guid AppDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
    public string ConfigurationJson { get; private set; } = string.Empty;

    private PageDefinition() { }

    public static Result<PageDefinition> Create(
        Guid AppDefinitionId,
        string name,
        string route,
        string configurationJson,
        IDateTimeProvider dateTimeProvider)
    {
        if (AppDefinitionId == Guid.Empty)
            return Result<PageDefinition>.Failure(
                Error.Validation("AppDefinition.Page.AppDefinitionId", "Application definition ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<PageDefinition>.Failure(nameResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<PageDefinition>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        return Result<PageDefinition>.Success(new PageDefinition
        {
            Id = Guid.NewGuid(),
            AppDefinitionId = AppDefinitionId,
            Name = name.Trim(),
            Route = route?.Trim() ?? string.Empty,
            ConfigurationJson = configurationJson ?? "{}",
            CreatedAt = now
        });
    }

    public Result Update(string name, string route, string configurationJson, IDateTimeProvider dateTimeProvider)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return nameResult;
        Name = name.Trim();
        Route = route?.Trim() ?? string.Empty;
        ConfigurationJson = configurationJson ?? "{}";
        UpdatedAt = dateTimeProvider?.UtcNow ?? UpdatedAt;
        return Result.Success();
    }
}
