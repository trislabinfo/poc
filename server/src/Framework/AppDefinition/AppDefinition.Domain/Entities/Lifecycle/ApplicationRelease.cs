using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace AppDefinition.Domain.Entities.Lifecycle;

/// <summary>
/// Immutable snapshot of an application at a version (shared by AppBuilder and TenantApplication).
/// AppDefinitionId = parent app id (AppBuilder: application definition; TenantApplication: tenant application id).
/// </summary>
public sealed class ApplicationRelease : Entity<Guid>
{
    public Guid AppDefinitionId { get; private set; }
    public string Version { get; private set; } = string.Empty;
    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Patch { get; private set; }
    public string ReleaseNotes { get; private set; } = string.Empty;
    public string NavigationJson { get; private set; } = "{}";
    public string PageJson { get; private set; } = "{}";
    public string DataSourceJson { get; private set; } = "{}";
    public string EntityJson { get; private set; } = "{}";
    public string SchemaJson { get; private set; } = "{}";
    public string DdlScriptsJson { get; private set; } = "{}";
    public DdlScriptStatus DdlScriptsStatus { get; private set; } = DdlScriptStatus.Pending;
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime ReleasedAt { get; private set; }
    public Guid ReleasedBy { get; private set; }
    public bool IsActive { get; private set; }
    /// <summary>Generated initial-view HTML (navigation + shell). Set when the app is released.</summary>
    public string? InitialViewHtml { get; private set; }

    private ApplicationRelease() { }

    public static Result<ApplicationRelease> Create(
        Guid AppDefinitionId,
        string version,
        int major,
        int minor,
        int patch,
        string releaseNotes,
        string navigationJson,
        string pageJson,
        string dataSourceJson,
        string entityJson,
        string schemaJson,
        string ddlScriptsJson,
        Guid releasedBy,
        IDateTimeProvider dateTimeProvider)
    {
        if (AppDefinitionId == Guid.Empty)
            return Result<ApplicationRelease>.Failure(
                Error.Validation("AppDefinition.Release.AppDefinitionId", "Application definition ID is required."));
        var versionResult = Guard.Against.NullOrWhiteSpace(version, nameof(version));
        if (versionResult.IsFailure) return Result<ApplicationRelease>.Failure(versionResult.Error);
        if (major < 0 || minor < 0 || patch < 0)
            return Result<ApplicationRelease>.Failure(
                Error.Validation("AppDefinition.Release.VersionNumbers", "Major, minor and patch must be non-negative."));
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<ApplicationRelease>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        return Result<ApplicationRelease>.Success(new ApplicationRelease
        {
            Id = Guid.NewGuid(),
            AppDefinitionId = AppDefinitionId,
            Version = version.Trim(),
            Major = major,
            Minor = minor,
            Patch = patch,
            ReleaseNotes = releaseNotes ?? string.Empty,
            NavigationJson = navigationJson ?? "{}",
            PageJson = pageJson ?? "{}",
            DataSourceJson = dataSourceJson ?? "{}",
            EntityJson = entityJson ?? "{}",
            SchemaJson = schemaJson ?? "{}",
            DdlScriptsJson = ddlScriptsJson ?? "{}",
            DdlScriptsStatus = DdlScriptStatus.Pending,
            ReleasedAt = now,
            ReleasedBy = releasedBy,
            IsActive = true,
            CreatedAt = now
        });
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>Sets the generated initial-view HTML. Called when the app is released (after HTML generation).</summary>
    public void SetInitialViewHtml(string? initialViewHtml) => InitialViewHtml = initialViewHtml;

    public void SetDdlScripts(string ddlScriptsJson)
    {
        if (DdlScriptsStatus == DdlScriptStatus.Approved)
            throw new InvalidOperationException("Cannot modify DDL scripts of an approved release.");
        DdlScriptsJson = ddlScriptsJson ?? "{}";
        DdlScriptsStatus = DdlScriptStatus.Pending;
    }

    public Result ApproveDdlScripts(Guid approvedBy, IDateTimeProvider dateTimeProvider)
    {
        if (DdlScriptsStatus != DdlScriptStatus.Pending)
            return Result.Failure(Error.Validation("ApplicationRelease.DdlScriptsStatus", "Only pending scripts can be approved."));

        DdlScriptsStatus = DdlScriptStatus.Approved;
        ApprovedAt = dateTimeProvider.UtcNow;
        ApprovedBy = approvedBy;
        return Result.Success();
    }

    public Result RejectDdlScripts(IDateTimeProvider dateTimeProvider)
    {
        if (DdlScriptsStatus != DdlScriptStatus.Pending)
            return Result.Failure(Error.Validation("ApplicationRelease.DdlScriptsStatus", "Only pending scripts can be rejected."));

        DdlScriptsStatus = DdlScriptStatus.Rejected;
        return Result.Success();
    }
}

/// <summary>Status of DDL scripts in a release.</summary>
public enum DdlScriptStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
