using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

using Identity.Domain.Events;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

/// <summary>
/// User aggregate root. Represents a user account with default tenant, email, display name and active state.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    /// <summary>Default tenant for this user.</summary>
    public Guid DefaultTenantId { get; private set; }
    /// <summary>User email (unique).</summary>
    public Email Email { get; private set; } = default!;
    /// <summary>Display name.</summary>
    public string DisplayName { get; private set; } = string.Empty;
    /// <summary>Whether the user account is active.</summary>
    public bool IsActive { get; private set; }

    private User()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="defaultTenantId">Default tenant ID for the user.</param>
    /// <param name="email">User email (value object).</param>
    /// <param name="displayName">Display name (1–100 characters).</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created user or a validation error.</returns>
    public static Result<User> Create(
        Guid defaultTenantId,
        Email email,
        string displayName,
        IDateTimeProvider dateTimeProvider)
    {
        var defaultTenantIdResult = Guard.Against.EmptyGuid(defaultTenantId, nameof(defaultTenantId));
        if (defaultTenantIdResult.IsFailure)
            return Result<User>.Failure(defaultTenantIdResult.Error);

        var emailResult = Guard.Against.Null(email, nameof(email));
        if (emailResult.IsFailure)
            return Result<User>.Failure(emailResult.Error);

        var displayNameResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        if (displayNameResult.IsFailure)
            return Result<User>.Failure(displayNameResult.Error);

        if (displayName.Length > 100)
        {
            return Result<User>.Failure(
                Error.Validation("Identity.User.DisplayNameTooLong", "Display name cannot exceed 100 characters."));
        }

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<User>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            DefaultTenantId = defaultTenantId,
            Email = email,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = now
        };

        user.RaiseDomainEvent(new UserCreatedEvent(
            user.Id,
            user.DefaultTenantId,
            user.Email.Value,
            now));

        return Result<User>.Success(user);
    }

    /// <summary>
    /// Updates the user's display name.
    /// </summary>
    /// <param name="displayName">New display name (1–100 characters).</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Success or a validation error.</returns>
    public Result Update(string displayName, IDateTimeProvider dateTimeProvider)
    {
        var displayNameResult = Guard.Against.NullOrWhiteSpace(displayName, nameof(displayName));
        if (displayNameResult.IsFailure)
            return displayNameResult;

        if (displayName.Length > 100)
        {
            return Result.Failure(
                Error.Validation("Identity.User.DisplayNameTooLong", "Display name cannot exceed 100 characters."));
        }

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return dateTimeProviderResult;

        DisplayName = displayName;
        UpdatedAt = dateTimeProvider.UtcNow;

        RaiseDomainEvent(new UserUpdatedEvent(Id, dateTimeProvider.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the user account.
    /// </summary>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Success or failure if already deactivated.</returns>
    public Result Deactivate(IDateTimeProvider dateTimeProvider)
    {
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return dateTimeProviderResult;

        if (!IsActive)
        {
            return Result.Failure(
                Error.Failure("Identity.User.AlreadyDeactivated", "User is already deactivated."));
        }

        IsActive = false;
        UpdatedAt = dateTimeProvider.UtcNow;

        RaiseDomainEvent(new UserDeactivatedEvent(Id, dateTimeProvider.UtcNow));

        return Result.Success();
    }

    #region Future Properties - Phase 2
    // TODO Phase 2: public ICollection<Guid> TenantIds { get; private set; } // user is member of one or more tenants
    // TODO Phase 2: public Result SetDefaultTenant(Guid tenantId)
    // TODO Phase 2: public bool EmailConfirmed { get; private set; }
    // TODO Phase 2: public bool IsLocked { get; private set; }
    // TODO Phase 2: public DateTime? LockoutEnd { get; private set; }
    // TODO Phase 2: public int FailedLoginAttempts { get; private set; }
    // TODO Phase 3: public DateTime? LastPasswordChangedAt { get; private set; }
    // TODO Phase 3: public DateTime? PasswordExpiresAt { get; private set; }
    // TODO Phase 4: public DateTime? DeletedAt { get; private set; }
    // TODO Phase 4: public byte[] RowVersion { get; private set; }
    #endregion

    #region Future Methods - Phase 2
    // TODO Phase 2: public Result ConfirmEmail()
    // TODO Phase 2: public Result Lock(string reason, TimeSpan? duration)
    // TODO Phase 2: public Result Unlock()
    // TODO Phase 2: public Result RecordFailedLogin()
    // TODO Phase 2: public Result ResetFailedLoginAttempts()
    // TODO Phase 3: public Result ChangePassword()
    // TODO Phase 4: public Result MarkForDeletion()
    // TODO Phase 4: public Result AnonymizePersonalData()
    #endregion
}

