using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Password hash value object.
/// </summary>
public sealed class PasswordHash : ValueObject
{
    /// <summary>
    /// Hash value.
    /// </summary>
    public string Hash { get; }

    private PasswordHash(string hash)
    {
        Hash = hash;
    }

    /// <summary>
    /// Creates a <see cref="PasswordHash"/> instance after validation.
    /// </summary>
    /// <param name="hash">Password hash.</param>
    /// <returns>Success with <see cref="PasswordHash"/> or failure with validation error.</returns>
    public static Result<PasswordHash> Create(string? hash)
    {
        try
        {
            Guard.Against.NullOrEmpty(hash ?? string.Empty, nameof(hash));
        }
        catch (ArgumentException)
        {
            return Result<PasswordHash>.Failure(Error.Validation("Identity.PasswordHash.Empty", "Invalid password hash"));
        }

        // TODO Phase 3: Algorithm tracking (bcrypt, argon2)
        // TODO Phase 3: Work factor tracking
        // TODO Phase 3: NeedsRehash() method for algorithm upgrades

        return Result<PasswordHash>.Success(new PasswordHash(hash!));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Hash;
    }
}

