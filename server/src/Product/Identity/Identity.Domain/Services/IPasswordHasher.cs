namespace Identity.Domain.Services;

/// <summary>
/// Service for hashing and verifying passwords.
/// </summary>
/// <remarks>
/// Implementation should be in Infrastructure layer using BCrypt or Argon2.
/// </remarks>
public interface IPasswordHasher
{
    /// <summary>
    /// Hash a plain text password.
    /// </summary>
    /// <param name="password">Plain text password.</param>
    /// <returns>Hashed password.</returns>
    string Hash(string password);

    /// <summary>
    /// Verify a plain text password against a hash.
    /// </summary>
    /// <param name="password">Plain text password.</param>
    /// <param name="hash">Stored hash.</param>
    /// <returns>True when password matches hash; otherwise false.</returns>
    bool Verify(string password, string hash);

    #region Future Methods - Phase 3
    // TODO Phase 3: string HashWithAlgorithm(string password, HashAlgorithm algorithm);
    // TODO Phase 3: bool NeedsRehash(string hash);
    #endregion
}

