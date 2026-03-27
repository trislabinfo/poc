using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// User-role assignment entity. Links a user to a role with an assignment timestamp.
/// </summary>
public sealed class UserRole : Entity<Guid>
{
    /// <summary>User ID.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Role ID.</summary>
    public Guid RoleId { get; private set; }
    /// <summary>When the role was assigned (UTC).</summary>
    public DateTime AssignedAt { get; private set; }

    private UserRole()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new user-role assignment.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="roleId">Role ID.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created user-role or a validation error.</returns>
    public static Result<UserRole> Create(
        Guid userId,
        Guid roleId,
        IDateTimeProvider dateTimeProvider)
    {
        var userIdResult = Guard.Against.EmptyGuid(userId, nameof(userId));
        if (userIdResult.IsFailure)
            return Result<UserRole>.Failure(userIdResult.Error);

        var roleIdResult = Guard.Against.EmptyGuid(roleId, nameof(roleId));
        if (roleIdResult.IsFailure)
            return Result<UserRole>.Failure(roleIdResult.Error);

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<UserRole>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = roleId,
            AssignedAt = now,
            CreatedAt = now
        };

        userRole.RaiseDomainEvent(new UserRoleAssignedEvent(
            userId,
            roleId,
            now));

        return Result<UserRole>.Success(userRole);
    }

    #region Future Properties - Phase 3
    // TODO Phase 3: public DateTime? ExpiresAt { get; private set; }
    // TODO Phase 3: public Guid? AssignedBy { get; private set; }
    // TODO Phase 3: public bool IsConditional { get; private set; }
    #endregion

    #region Future Methods - Phase 3
    // TODO Phase 3: public bool IsExpired()
    // TODO Phase 3: public Result Extend(TimeSpan duration)
    #endregion
}

