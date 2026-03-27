using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// Role aggregate root. Represents a tenant-scoped role with name, description and system-role flag.
/// </summary>
public sealed class Role : AggregateRoot<Guid>
{
    /// <summary>Tenant that owns this role.</summary>
    public Guid TenantId { get; private set; }
    /// <summary>Role name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Role description.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>Whether this is a system role (e.g. not editable).</summary>
    public bool IsSystemRole { get; private set; }
    /// <summary>Whether the role is active.</summary>
    public bool IsActive { get; private set; }

    private Role()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new role for a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <param name="name">Role name.</param>
    /// <param name="description">Role description.</param>
    /// <param name="isSystemRole">True if this is a system role.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created role or a validation error.</returns>
    public static Result<Role> Create(
        Guid tenantId,
        string name,
        string description,
        bool isSystemRole,
        IDateTimeProvider dateTimeProvider)
    {
        var tenantIdResult = Guard.Against.EmptyGuid(tenantId, nameof(tenantId));
        if (tenantIdResult.IsFailure)
            return Result<Role>.Failure(tenantIdResult.Error);

        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure)
            return Result<Role>.Failure(nameResult.Error);

        var descriptionResult = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        if (descriptionResult.IsFailure)
            return Result<Role>.Failure(descriptionResult.Error);

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<Role>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            Description = description,
            IsSystemRole = isSystemRole,
            IsActive = true,
            CreatedAt = now
        };

        role.RaiseDomainEvent(new RoleCreatedEvent(
            role.Id,
            role.TenantId,
            role.Name,
            now));

        return Result<Role>.Success(role);
    }

    /// <summary>
    /// Updates the role name and description. System roles cannot be updated.
    /// </summary>
    /// <param name="name">New name.</param>
    /// <param name="description">New description.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Success or a validation/domain error.</returns>
    public Result Update(string name, string description, IDateTimeProvider dateTimeProvider)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure)
            return nameResult;

        var descriptionResult = Guard.Against.NullOrWhiteSpace(description, nameof(description));
        if (descriptionResult.IsFailure)
            return descriptionResult;

        if (IsSystemRole)
        {
            return Result.Failure(
                Error.Failure("Identity.Role.CannotUpdateSystemRole", "Cannot update system role."));
        }

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return dateTimeProviderResult;

        Name = name;
        Description = description;
        UpdatedAt = dateTimeProvider.UtcNow;

        RaiseDomainEvent(new RoleUpdatedEvent(Id, dateTimeProvider.UtcNow));

        return Result.Success();
    }

    #region Future Properties - Phase 2
    // TODO Phase 2: public ICollection<RolePermission> Permissions { get; private set; }
    // TODO Phase 3: public Guid? ParentRoleId { get; private set; }
    // TODO Phase 4: public byte[] RowVersion { get; private set; }
    #endregion

    #region Future Methods - Phase 2
    // TODO Phase 2: public Result AddPermission(Guid permissionId)
    // TODO Phase 2: public Result RemovePermission(Guid permissionId)
    // TODO Phase 2: public bool HasPermission(string permissionCode)
    #endregion
}

