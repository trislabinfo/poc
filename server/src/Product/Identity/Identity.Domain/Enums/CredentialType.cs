namespace Identity.Domain.Enums;

/// <summary>
/// Types of credentials a user can have.
/// </summary>
public enum CredentialType
{
    /// <summary>
    /// Password-based authentication.
    /// </summary>
    Password = 1,

    /// <summary>
    /// External provider authentication (Google, Microsoft, etc.).
    /// </summary>
    /// <remarks>TODO Phase 3: Implement external provider authentication</remarks>
    ExternalProvider = 2,

    /// <summary>
    /// Multi-factor authentication.
    /// </summary>
    /// <remarks>TODO Phase 3: Implement MFA</remarks>
    Mfa = 3
}

// TODO Phase 2: ConsentType (TermsOfService, DataProcessing, Marketing)
// TODO Phase 2: FailureReason (InvalidPassword, AccountLocked, EmailNotConfirmed)
// TODO Phase 2: LockoutReason (TooManyFailedAttempts, AdminAction, SecurityBreach)

