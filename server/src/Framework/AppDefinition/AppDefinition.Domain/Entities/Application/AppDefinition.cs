using AppBuilder.Domain.Enums;
using AppDefinition.Domain.Entities.Lifecycle;
using AppDefinition.Domain.Events;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using System.Text.RegularExpressions;

namespace AppDefinition.Domain.Entities.Application;

/// <summary>
/// Aggregate root for a no-code application being built. Owns releases and lifecycle (Draft → Released → Archived).
/// </summary>
public sealed class AppDefinition : AggregateRoot<Guid>
{
    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Application display name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Documentation.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>URL-friendly identifier (unique).</summary>
    public string Slug { get; private set; } = string.Empty;
    /// <summary>Lifecycle status.</summary>
    public ApplicationStatus Status { get; private set; }
    /// <summary>Latest released version (e.g. "1.0.0"), null if never released.</summary>
    public string? CurrentVersion { get; private set; }
    /// <summary>Whether visible in tenant catalog.</summary>
    public bool IsPublic { get; private set; }

    private AppDefinition()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new application definition in Draft status.
    /// </summary>
    public static Result<AppDefinition> Create(
        string name,
        string description,
        string slug,
        bool isPublic,
        IDateTimeProvider dateTimeProvider)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure)
            return Result<AppDefinition>.Failure(nameResult.Error);

        var slugResult = Guard.Against.NullOrWhiteSpace(slug, nameof(slug));
        if (slugResult.IsFailure)
            return Result<AppDefinition>.Failure(slugResult.Error);

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (normalizedSlug.Length > 100)
            return Result<AppDefinition>.Failure(
                Error.Validation("AppBuilder.SlugTooLong", "Slug cannot exceed 100 characters."));
        if (!SlugRegex.IsMatch(normalizedSlug))
            return Result<AppDefinition>.Failure(
                Error.Validation("AppBuilder.InvalidSlugFormat", "Slug must contain only lowercase letters, numbers, and hyphens."));

        if (name.Trim().Length > 200)
            return Result<AppDefinition>.Failure(
                Error.Validation("AppBuilder.NameTooLong", "Name cannot exceed 200 characters."));
        if (description.Length > 1000)
            return Result<AppDefinition>.Failure(
                Error.Validation("AppBuilder.DescriptionTooLong", "Description cannot exceed 1000 characters."));

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<AppDefinition>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        var definition = new AppDefinition
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description ?? string.Empty,
            Slug = normalizedSlug,
            Status = ApplicationStatus.Draft,
            IsPublic = isPublic,
            CreatedAt = now
        };

        definition.RaiseDomainEvent(new AppDefinitionCreatedEvent(
            definition.Id,
            definition.Name,
            definition.Slug,
            now));

        return Result<AppDefinition>.Success(definition);
    }

    /// <summary>
    /// Updates name and description. Allowed only in Draft.
    /// </summary>
    public Result Update(string name, string description, IDateTimeProvider dateTimeProvider)
    {
        if (Status != ApplicationStatus.Draft)
            return Result.Failure(Error.Validation("AppBuilder.InvalidStatus", "Only draft applications can be updated."));

        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure)
            return nameResult;
        if (name.Trim().Length > 200)
            return Result.Failure(Error.Validation("AppBuilder.NameTooLong", "Name cannot exceed 200 characters."));
        if (description.Length > 1000)
            return Result.Failure(Error.Validation("AppBuilder.DescriptionTooLong", "Description cannot exceed 1000 characters."));

        Name = name.Trim();
        Description = description ?? string.Empty;
        UpdatedAt = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider)).IsSuccess
            ? dateTimeProvider.UtcNow
            : UpdatedAt;
        return Result.Success();
    }

    /// <summary>
    /// Creates an immutable release. Sets Status to Released and CurrentVersion.
    /// </summary>
    public Result<ApplicationRelease> CreateRelease(
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
        if (Status != ApplicationStatus.Draft)
            return Result<ApplicationRelease>.Failure(
                Error.Validation("AppBuilder.InvalidStatus", "Only draft applications can be released."));

        var versionResult = Guard.Against.NullOrWhiteSpace(version, nameof(version));
        if (versionResult.IsFailure)
            return Result<ApplicationRelease>.Failure(versionResult.Error);

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<ApplicationRelease>.Failure(dateTimeProviderResult.Error);

        var releaseResult = ApplicationRelease.Create(
            Id, version, major, minor, patch,
            releaseNotes ?? string.Empty,
            navigationJson ?? "{}", pageJson ?? "{}", dataSourceJson ?? "{}", entityJson ?? "{}", schemaJson ?? "{}", ddlScriptsJson ?? "{}",
            releasedBy, dateTimeProvider);
        if (releaseResult.IsFailure)
            return Result<ApplicationRelease>.Failure(releaseResult.Error);

        var release = releaseResult.Value;
        Status = ApplicationStatus.Released;
        CurrentVersion = version.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;

        RaiseDomainEvent(new ApplicationReleasedEvent(Id, release.Id, release.Version, dateTimeProvider.UtcNow));
        return Result<ApplicationRelease>.Success(release);
    }

    /// <summary>
    /// Marks the application as archived.
    /// </summary>
    public Result Archive(IDateTimeProvider dateTimeProvider)
    {
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return dateTimeProviderResult;
        Status = ApplicationStatus.Archived;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }
}
