using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

using Identity.Domain.Enums;
using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

/// <summary>
/// User credential (password, external provider, or MFA) linked to a user.
/// </summary>
public sealed class Credential : Entity<Guid>
{
    /// <summary>User that owns this credential.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Type of credential (e.g. Password).</summary>
    public CredentialType Type { get; private set; }
    /// <summary>Hashed secret (e.g. password hash).</summary>
    public PasswordHash PasswordHash { get; private set; } = default!;
    /// <summary>Whether this credential can be used for authentication.</summary>
    public bool IsActive { get; private set; }

    private Credential()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new password credential for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="passwordHash">Hashed password.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created credential or a validation error.</returns>
    public static Result<Credential> CreatePassword(
        Guid userId,
        PasswordHash passwordHash,
        IDateTimeProvider dateTimeProvider)
    {
        var userIdResult = Guard.Against.EmptyGuid(userId, nameof(userId));
        if (userIdResult.IsFailure)
            return Result<Credential>.Failure(userIdResult.Error);

        var passwordHashResult = Guard.Against.Null(passwordHash, nameof(passwordHash));
        if (passwordHashResult.IsFailure)
            return Result<Credential>.Failure(passwordHashResult.Error);

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<Credential>.Failure(dateTimeProviderResult.Error);

        var credential = new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = CredentialType.Password,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = dateTimeProvider.UtcNow
        };

        return Result<Credential>.Success(credential);
    }

    /// <summary>
    /// Updates the password hash for this credential.
    /// </summary>
    /// <param name="newPasswordHash">New hashed password.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Success or a validation error.</returns>
    public Result UpdatePassword(PasswordHash newPasswordHash, IDateTimeProvider dateTimeProvider)
    {
        var passwordHashResult = Guard.Against.Null(newPasswordHash, nameof(newPasswordHash));
        if (passwordHashResult.IsFailure)
            return passwordHashResult;

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return dateTimeProviderResult;

        PasswordHash = newPasswordHash;
        UpdatedAt = dateTimeProvider.UtcNow;

        return Result.Success();
    }

    #region Future Properties - Phase 3
    // TODO Phase 3: public string? ExternalProviderId { get; private set; }
    // TODO Phase 3: public string? ExternalProvider { get; private set; }
    // TODO Phase 3: public EncryptedValue<string>? MfaSecret { get; private set; }
    // TODO Phase 3: public EncryptedValue<string[]>? MfaBackupCodes { get; private set; }
    // TODO Phase 3: public DateTime? LastUsedAt { get; private set; }
    #endregion

    #region Future Methods - Phase 3
    // TODO Phase 3: public static Result<Credential> CreateExternalProvider(...)
    // TODO Phase 3: public static Result<Credential> CreateMfa(...)
    // TODO Phase 3: public Result RecordUsage()
    #endregion
}

