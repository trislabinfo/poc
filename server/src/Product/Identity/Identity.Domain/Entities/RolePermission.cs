using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace Identity.Domain.Entities;

/// <summary>
/// Role-permission assignment entity. Links a role to a permission with a grant timestamp.
/// </summary>
public sealed class RolePermission : Entity<Guid>
{
    /// <summary>Role ID.</summary>
    public Guid RoleId { get; private set; }
    /// <summary>Permission ID.</summary>
    public Guid PermissionId { get; private set; }
    /// <summary>When the permission was granted (UTC).</summary>
    public DateTime GrantedAt { get; private set; }

    private RolePermission()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new role-permission assignment.
    /// </summary>
    /// <param name="roleId">Role ID.</param>
    /// <param name="permissionId">Permission ID.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created role-permission or a validation error.</returns>
    public static Result<RolePermission> Create(
        Guid roleId,
        Guid permissionId,
        IDateTimeProvider dateTimeProvider)
    {
        var roleIdResult = Guard.Against.EmptyGuid(roleId, nameof(roleId));
        if (roleIdResult.IsFailure)
            return Result<RolePermission>.Failure(roleIdResult.Error);

        var permissionIdResult = Guard.Against.EmptyGuid(permissionId, nameof(permissionId));
        if (permissionIdResult.IsFailure)
            return Result<RolePermission>.Failure(permissionIdResult.Error);

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<RolePermission>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = permissionId,
            GrantedAt = now,
            CreatedAt = now
        };

        return Result<RolePermission>.Success(rolePermission);
    }

    #region Future Properties - Phase 3
    // TODO Phase 3: public DateTime? ExpiresAt { get; private set; }
    // TODO Phase 3: public string? Constraints { get; private set; } // JSON constraints
    // TODO Phase 3: public Guid? GrantedBy { get; private set; }
    #endregion

    #region Future Methods - Phase 3
    // TODO Phase 3: public bool IsExpired()
    // TODO Phase 3: public bool MeetsConstraints(object context)
    #endregion
}

