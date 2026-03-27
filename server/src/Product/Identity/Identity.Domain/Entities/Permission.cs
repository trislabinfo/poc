using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Identity.Domain.Events;
using System.Text.RegularExpressions;

namespace Identity.Domain.Entities;

/// <summary>
/// Permission aggregate root. Represents a permission with code (module.action), name, description and module.
/// </summary>
public sealed class Permission : AggregateRoot<Guid>
{
    private static readonly Regex CodeRegex = new(
        @"^[a-z]+\.[a-z]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Permission code (e.g. "users.read").</summary>
    public string Code { get; private set; } = string.Empty;
    /// <summary>Display name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Description.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>Module this permission belongs to.</summary>
    public string Module { get; private set; } = string.Empty;

    private Permission()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    /// <param name="code">Code in format "module.action".</param>
    /// <param name="name">Display name.</param>
    /// <param name="description">Description.</param>
    /// <param name="module">Module name.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created permission or a validation error.</returns>
    public static Result<Permission> Create(
        string code,
        string name,
        string description,
        string module,
        IDateTimeProvider dateTimeProvider)
    {
        var codeResult = Guard.Against.NullOrWhiteSpace(code, nameof(code));
        if (codeResult.IsFailure)
            return Result<Permission>.Failure(codeResult.Error);

        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure)
            return Result<Permission>.Failure(nameResult.Error);

        var descriptionResult = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        if (descriptionResult.IsFailure)
            return Result<Permission>.Failure(descriptionResult.Error);

        var moduleResult = Guard.Against.NullOrWhiteSpace(module, nameof(module));
        if (moduleResult.IsFailure)
            return Result<Permission>.Failure(moduleResult.Error);

        if (!CodeRegex.IsMatch(code))
        {
            return Result<Permission>.Failure(
                Error.Validation(
                    "Identity.Permission.InvalidCodeFormat",
                    "Permission code must be in format 'module.action'."));
        }

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<Permission>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            Description = description,
            Module = module,
            CreatedAt = now
        };

        permission.RaiseDomainEvent(new PermissionCreatedEvent(
            permission.Id,
            permission.Code,
            now));

        return Result<Permission>.Success(permission);
    }

    #region Future Properties - Phase 3
    // TODO Phase 3: public ICollection<Guid> RequiredPermissions { get; private set; }
    // TODO Phase 3: public string? ResourcePattern { get; private set; } // for resource-level permissions
    // TODO Phase 3: public bool IsDynamic { get; private set; }
    #endregion

    #region Future Methods - Phase 3
    // TODO Phase 3: public Result AddRequiredPermission(Guid permissionId)
    // TODO Phase 3: public bool MatchesResource(string resourceId)
    #endregion
}

