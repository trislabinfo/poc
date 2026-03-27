using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using System.Text.RegularExpressions;
using TenantApplication.Domain.Enums;

namespace TenantApplication.Domain.Entities;

/// <summary>Tenant's installed or custom application (aggregate root).</summary>
public sealed class TenantApplication : AggregateRoot<Guid>
{
    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly List<TenantApplicationEnvironment> _environments = [];

    public IReadOnlyCollection<TenantApplicationEnvironment> Environments => _environments.AsReadOnly();

    public Guid TenantId { get; private set; }
    public Guid? ApplicationReleaseId { get; private set; }
    public Guid? ApplicationId { get; private set; }
    public int? Major { get; private set; }
    public int? Minor { get; private set; }
    public int? Patch { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsCustom { get; private set; }
    public Guid? SourceApplicationReleaseId { get; private set; }
    public TenantApplicationStatus Status { get; private set; }
    public string ConfigurationJson { get; private set; } = "{}";
    public DateTime? InstalledAt { get; private set; }
    public DateTime? ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }

    public DateTime? UninstalledAt { get; private set; }

    private TenantApplication() { }

    public static Result<TenantApplication> InstallFromPlatform(
        Guid tenantId,
        Guid applicationReleaseId,
        string name,
        string slug,
        IDateTimeProvider dateTimeProvider)
    {
        if (tenantId == Guid.Empty)
            return Result<TenantApplication>.Failure(
                Error.Validation("TenantApplication.TenantId", "Tenant ID is required."));
        if (applicationReleaseId == Guid.Empty)
            return Result<TenantApplication>.Failure(
                Error.Validation("TenantApplication.ApplicationReleaseId", "Application release ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<TenantApplication>.Failure(nameResult.Error);
        var slugResult = ValidateSlug(slug);
        if (slugResult.IsFailure) return Result<TenantApplication>.Failure(slugResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<TenantApplication>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        var app = new TenantApplication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ApplicationReleaseId = applicationReleaseId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            IsCustom = false,
            Status = TenantApplicationStatus.Installed,
            ConfigurationJson = "{}",
            InstalledAt = now,
            CreatedAt = now
        };
        return Result<TenantApplication>.Success(app);
    }

    public static Result<TenantApplication> CreateCustom(
        Guid tenantId,
        string name,
        string slug,
        string? description,
        IDateTimeProvider dateTimeProvider)
    {
        if (tenantId == Guid.Empty)
            return Result<TenantApplication>.Failure(
                Error.Validation("TenantApplication.TenantId", "Tenant ID is required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<TenantApplication>.Failure(nameResult.Error);
        var slugResult = ValidateSlug(slug);
        if (slugResult.IsFailure) return Result<TenantApplication>.Failure(slugResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<TenantApplication>.Failure(dateTimeProviderResult.Error);
        if (description != null && description.Length > 1000)
            return Result<TenantApplication>.Failure(
                Error.Validation("TenantApplication.DescriptionTooLong", "Description cannot exceed 1000 characters."));

        var now = dateTimeProvider.UtcNow;
        var app = new TenantApplication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            IsCustom = true,
            Status = TenantApplicationStatus.Draft,
            ConfigurationJson = "{}",
            CreatedAt = now
        };
        return Result<TenantApplication>.Success(app);
    }

    public static Result<TenantApplication> ForkFromPlatform(
        Guid tenantId,
        Guid sourceApplicationReleaseId,
        string name,
        string slug,
        IDateTimeProvider dateTimeProvider)
    {
        if (tenantId == Guid.Empty || sourceApplicationReleaseId == Guid.Empty)
            return Result<TenantApplication>.Failure(
                Error.Validation("TenantApplication.Fork", "Tenant ID and source release ID are required."));
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<TenantApplication>.Failure(nameResult.Error);
        var slugResult = ValidateSlug(slug);
        if (slugResult.IsFailure) return Result<TenantApplication>.Failure(slugResult.Error);
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure) return Result<TenantApplication>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        var app = new TenantApplication
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SourceApplicationReleaseId = sourceApplicationReleaseId,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            IsCustom = true,
            Status = TenantApplicationStatus.Draft,
            ConfigurationJson = "{}",
            CreatedAt = now
        };
        return Result<TenantApplication>.Success(app);
    }

    private static Result ValidateSlug(string slug)
    {
        var slugResult = Guard.Against.NullOrWhiteSpace(slug, nameof(slug));
        if (slugResult.IsFailure) return slugResult;
        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length > 100)
            return Result.Failure(Error.Validation("TenantApplication.SlugTooLong", "Slug cannot exceed 100 characters."));
        if (!SlugRegex.IsMatch(normalized))
            return Result.Failure(Error.Validation("TenantApplication.InvalidSlugFormat", "Slug must contain only lowercase letters, numbers, and hyphens."));
        return Result.Success();
    }

    public Result<TenantApplicationEnvironment> CreateEnvironment(string name, EnvironmentType environmentType, IDateTimeProvider dateTimeProvider)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure) return Result<TenantApplicationEnvironment>.Failure(nameResult.Error);
        if (_environments.Any(e => e.EnvironmentType == environmentType))
            return Result<TenantApplicationEnvironment>.Failure(
                Error.Validation("TenantApplication.Environment", "An environment of this type already exists."));
        var envResult = TenantApplicationEnvironment.Create(Id, name, environmentType, dateTimeProvider);
        if (envResult.IsFailure) return Result<TenantApplicationEnvironment>.Failure(envResult.Error);
        _environments.Add(envResult.Value);
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result<TenantApplicationEnvironment>.Success(envResult.Value);
    }

    public Result RemoveEnvironment(Guid environmentId)
    {
        var env = _environments.FirstOrDefault(e => e.Id == environmentId);
        if (env == null)
            return Result.Failure(Error.NotFound("TenantApplication.EnvironmentNotFound", "Environment not found."));
        _environments.Remove(env);
        return Result.Success();
    }

    public void SetReleaseInfo(Guid? applicationReleaseId, Guid? applicationId, int? major, int? minor, int? patch)
    {
        ApplicationReleaseId = applicationReleaseId;
        ApplicationId = applicationId;
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public Result UpdateConfiguration(string configurationJson, IDateTimeProvider? dateTimeProvider = null)
    {
        ConfigurationJson = configurationJson ?? "{}";
        if (dateTimeProvider != null) UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }

    public Result Activate(IDateTimeProvider dateTimeProvider)
    {
        if (Status == TenantApplicationStatus.Archived)
            return Result.Failure(Error.Validation("TenantApplication.Status", "Cannot activate an archived application."));
        Status = TenantApplicationStatus.Active;
        ActivatedAt = dateTimeProvider.UtcNow;
        DeactivatedAt = null;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }

    public Result Deactivate(IDateTimeProvider dateTimeProvider)
    {
        Status = TenantApplicationStatus.Inactive;
        DeactivatedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }

    public Result Archive(IDateTimeProvider dateTimeProvider)
    {
        Status = TenantApplicationStatus.Archived;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }
}
