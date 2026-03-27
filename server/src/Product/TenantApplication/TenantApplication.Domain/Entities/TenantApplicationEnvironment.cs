using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Enums;

namespace TenantApplication.Domain.Entities;

/// <summary>Deployment environment for a tenant application.</summary>
public sealed class TenantApplicationEnvironment : Entity<Guid>
{
    public Guid TenantApplicationId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public EnvironmentType EnvironmentType { get; private set; }
    public Guid? ApplicationReleaseId { get; private set; }
    public string? ReleaseVersion { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? DeployedAt { get; private set; }
    public Guid? DeployedBy { get; private set; }
    public string? ConfigurationJson { get; private set; }
    public string? DatabaseName { get; private set; }
    public string? ConnectionString { get; private set; }

    private TenantApplicationEnvironment() { }

    public static Result<TenantApplicationEnvironment> Create(
        Guid tenantApplicationId,
        string name,
        EnvironmentType environmentType,
        IDateTimeProvider dateTimeProvider)
    {
        if (tenantApplicationId == Guid.Empty)
            return Result<TenantApplicationEnvironment>.Failure(
                Error.Validation("TenantApplicationEnvironment.TenantApplicationId", "Tenant application ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<TenantApplicationEnvironment>.Failure(nameResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<TenantApplicationEnvironment>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        return Result<TenantApplicationEnvironment>.Success(new TenantApplicationEnvironment
        {
            Id = Guid.NewGuid(),
            TenantApplicationId = tenantApplicationId,
            Name = name.Trim(),
            EnvironmentType = environmentType,
            CreatedAt = now
        });
    }

    public void DeployRelease(Guid releaseId, string version, Guid userId, IDateTimeProvider dateTimeProvider)
    {
        ApplicationReleaseId = releaseId;
        ReleaseVersion = version;
        DeployedAt = dateTimeProvider.UtcNow;
        DeployedBy = userId;
        IsActive = true;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    public void UpdateConfiguration(string? configurationJson, IDateTimeProvider? dateTimeProvider = null)
    {
        ConfigurationJson = configurationJson;
        if (dateTimeProvider != null) UpdatedAt = dateTimeProvider.UtcNow;
    }

    public void SetDatabaseInfo(string databaseName, string connectionString, IDateTimeProvider? dateTimeProvider = null)
    {
        DatabaseName = databaseName;
        ConnectionString = connectionString;
        if (dateTimeProvider != null) UpdatedAt = dateTimeProvider.UtcNow;
    }

    public void Activate(IDateTimeProvider dateTimeProvider)
    {
        IsActive = true;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    public void Deactivate(IDateTimeProvider dateTimeProvider)
    {
        IsActive = false;
        UpdatedAt = dateTimeProvider.UtcNow;
    }
}
