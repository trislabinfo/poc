using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Email address value object.
/// </summary>
public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Email address value.
    /// </summary>
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates an <see cref="Email"/> instance after validation.
    /// </summary>
    /// <param name="value">Email string.</param>
    /// <returns>Success with <see cref="Email"/> or failure with validation error.</returns>
    public static Result<Email> Create(string? value)
    {
        try
        {
            Guard.Against.NullOrEmpty(value ?? string.Empty, nameof(value));
        }
        catch (ArgumentException)
        {
            return Result<Email>.Failure(Error.Validation("Identity.Email.Empty", "Invalid email"));
        }

        if (value!.Length > 254)
        {
            return Result<Email>.Failure(Error.Validation("Identity.Email.TooLong", "Invalid email"));
        }

        if (!EmailRegex.IsMatch(value))
        {
            return Result<Email>.Failure(Error.Validation("Identity.Email.InvalidFormat", "Invalid email"));
        }

        // TODO Phase 2: Email normalization (lowercase, trim)
        // TODO Phase 3: Disposable email detection
        // TODO Phase 3: Domain blacklist/whitelist

        return Result<Email>.Success(new Email(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}

