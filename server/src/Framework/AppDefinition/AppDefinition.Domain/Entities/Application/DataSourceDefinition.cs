using AppDefinition.Domain.Enums;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppDefinition.Domain.Entities.Application;

/// <summary>Data source definition (shared by AppBuilder and TenantApplication).</summary>
public sealed class DataSourceDefinition : Entity<Guid>
{
    public Guid AppDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DataSourceType Type { get; private set; }
    public string ConfigurationJson { get; private set; } = string.Empty;

    private DataSourceDefinition() { }

    public static Result<DataSourceDefinition> Create(
        Guid AppDefinitionId,
        string name,
        DataSourceType type,
        string configurationJson,
        IDateTimeProvider dateTimeProvider)
    {
        if (AppDefinitionId == Guid.Empty)
            return Result<DataSourceDefinition>.Failure(
                Error.Validation("AppDefinition.DataSource.AppDefinitionId", "Application definition ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<DataSourceDefinition>.Failure(nameResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<DataSourceDefinition>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        return Result<DataSourceDefinition>.Success(new DataSourceDefinition
        {
            Id = Guid.NewGuid(),
            AppDefinitionId = AppDefinitionId,
            Name = name.Trim(),
            Type = type,
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
