using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// Refresh token entity. Represents a refresh token for a user with expiry and revocation support.
/// </summary>
public sealed class RefreshToken : Entity<Guid>
{
    /// <summary>User that owns this token.</summary>
    public Guid UserId { get; private set; }
    /// <summary>Token value (should be hashed by caller).</summary>
    public string Token { get; private set; } = string.Empty;
    /// <summary>UTC expiry time.</summary>
    public DateTime ExpiresAt { get; private set; }
    /// <summary>Whether the token has been revoked.</summary>
    public bool IsRevoked { get; private set; }
    /// <summary>When the token was revoked, if applicable.</summary>
    public DateTime? RevokedAt { get; private set; }

    private RefreshToken()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="token">Token value.</param>
    /// <param name="expiresIn">Lifetime of the token.</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created refresh token or a validation error.</returns>
    public static Result<RefreshToken> Create(
        Guid userId,
        string token,
        TimeSpan expiresIn,
        IDateTimeProvider dateTimeProvider)
    {
        var userIdResult = Guard.Against.EmptyGuid(userId, nameof(userId));
        if (userIdResult.IsFailure)
            return Result<RefreshToken>.Failure(userIdResult.Error);

        var tokenResult = Guard.Against.NullOrWhiteSpace(token, nameof(token));
        if (tokenResult.IsFailure)
            return Result<RefreshToken>.Failure(tokenResult.Error);

        if (expiresIn <= TimeSpan.Zero)
        {
            return Result<RefreshToken>.Failure(
                Error.Validation("Identity.RefreshToken.InvalidExpiry", "Expiry must be positive."));
        }

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<RefreshToken>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;
        var expiresAt = now.Add(expiresIn);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token, // Should be hashed by caller
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = now
        };

        refreshToken.RaiseDomainEvent(new RefreshTokenCreatedEvent(
            refreshToken.Id,
            userId,
            refreshToken.ExpiresAt,
            now));

        return Result<RefreshToken>.Success(refreshToken);
    }

    /// <summary>
    /// Returns whether this token is expired according to the given time provider.
    /// </summary>
    /// <param name="dateTimeProvider">Date/time provider.</param>
    /// <returns>True if current UTC time is after ExpiresAt.</returns>
    public bool IsExpired(IDateTimeProvider dateTimeProvider)
    {
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return true; // Treat invalid provider as expired to be safe
        return dateTimeProvider.UtcNow > ExpiresAt;
    }

    /// <summary>
    /// Revokes this refresh token.
    /// </summary>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Success or failure if already revoked.</returns>
    public Result Revoke(IDateTimeProvider dateTimeProvider)
    {
        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return dateTimeProviderResult;

        if (IsRevoked)
        {
            return Result.Failure(
                Error.Failure("Identity.RefreshToken.AlreadyRevoked", "Token is already revoked."));
        }

        var now = dateTimeProvider.UtcNow;

        IsRevoked = true;
        RevokedAt = now;
        UpdatedAt = now;

        RaiseDomainEvent(new RefreshTokenRevokedEvent(
            Id,
            UserId,
            now));

        return Result.Success();
    }

    #region Future Properties - Phase 3
    // TODO Phase 3: public Guid? ReplacedByToken { get; private set; }
    // TODO Phase 3: public string? DeviceInfo { get; private set; }
    // TODO Phase 3: public IpAddress? IpAddress { get; private set; }
    // TODO Phase 3: public string? RevocationReason { get; private set; }
    #endregion

    #region Future Methods - Phase 3
    // TODO Phase 3: public Result ReplaceWith(RefreshToken newToken)
    // TODO Phase 3: public bool IsPartOfStolenFamily()
    #endregion
}

