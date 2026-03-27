using BuildingBlocks.Kernel.Results;
using System.Text.RegularExpressions;

namespace BuildingBlocks.Kernel.Domain;

/// <summary>
/// Guard clauses for validating method inputs and invariants.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Guard methods for invalid arguments.
    /// </summary>
    public static class Against
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        #region Result-returning guards (no exceptions)

        /// <summary>
        /// Guards against null reference types. Returns Result instead of throwing.
        /// </summary>
        /// <typeparam name="T">The reference type to check.</typeparam>
        /// <param name="value">The value to check for null.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>Success if value is not null; Failure with validation error if null.</returns>
        public static Result Null<T>(T? value, string paramName) where T : class
        {
            if (value is null)
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.Null",
                    $"{paramName} cannot be null"));
            }
            return Result.Success();
        }

        /// <summary>
        /// Guards against null or whitespace strings. Returns Result instead of throwing.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>Success if value is not null or whitespace; Failure with validation error otherwise.</returns>
        public static Result NullOrWhiteSpace(string? value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.NullOrWhiteSpace",
                    $"{paramName} cannot be null or whitespace"));
            }
            return Result.Success();
        }

        /// <summary>
        /// Guards against invalid email addresses. Returns Result instead of throwing.
        /// </summary>
        /// <param name="email">The email address to validate.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>Success if email is valid; Failure with validation error if invalid.</returns>
        public static Result InvalidEmail(string? email, string paramName = "email")
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.NullOrWhiteSpace",
                    $"{paramName} cannot be null or whitespace"));
            }
            if (email.Length > 254)
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.TooLong",
                    $"{paramName} must not exceed 254 characters"));
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                if (addr.Address != email)
                {
                    return Result.Failure(Error.Validation(
                        $"{paramName}.Invalid",
                        $"{paramName} is not in a valid format"));
                }
            }
            catch
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.Invalid",
                    $"{paramName} is not in a valid format"));
            }
            return Result.Success();
        }

        /// <summary>
        /// Guards against values outside a specified range. Returns Result instead of throwing.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <param name="min">Minimum allowed value (inclusive).</param>
        /// <param name="max">Maximum allowed value (inclusive).</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>Success if value is in range; Failure with validation error if out of range.</returns>
        public static Result OutOfRange(int value, int min, int max, string paramName)
        {
            if (value < min || value > max)
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.OutOfRange",
                    $"{paramName} must be between {min} and {max}, but was {value}"));
            }
            return Result.Success();
        }

        /// <summary>
        /// Guards against strings with invalid length. Returns Result instead of throwing.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <param name="minLength">Minimum allowed length.</param>
        /// <param name="maxLength">Maximum allowed length.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>Success if length is valid; Failure with validation error if invalid.</returns>
        public static Result InvalidLength(string? value, int minLength, int maxLength, string paramName)
        {
            if (value is null)
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.Null",
                    $"{paramName} cannot be null"));
            }
            if (value.Length < minLength || value.Length > maxLength)
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.InvalidLength",
                    $"{paramName} length must be between {minLength} and {maxLength}, but was {value.Length}"));
            }
            return Result.Success();
        }

        /// <summary>
        /// Guards against empty GUIDs. Returns Result instead of throwing.
        /// </summary>
        /// <param name="value">The GUID to check.</param>
        /// <param name="paramName">The parameter name for error messages.</param>
        /// <returns>Success if GUID is not empty; Failure with validation error if empty.</returns>
        public static Result EmptyGuid(Guid value, string paramName)
        {
            if (value == Guid.Empty)
            {
                return Result.Failure(Error.Validation(
                    $"{paramName}.Empty",
                    $"{paramName} cannot be an empty GUID"));
            }
            return Result.Success();
        }

        #endregion

        #region Throw-based guards (legacy)

        /// <summary>
        /// Throws if <paramref name="value"/> is null or empty.
        /// </summary>
        /// <param name="value">String to validate.</param>
        /// <param name="parameterName">Parameter name.</param>
        /// <exception cref="ArgumentException">Thrown when the string is null or empty.</exception>
        public static void NullOrEmpty(string value, string parameterName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException($"Parameter '{parameterName}' cannot be null or empty.", parameterName);
            }
        }

        /// <summary>
        /// Throws if <paramref name="value"/> is outside the inclusive range [<paramref name="min"/>, <paramref name="max"/>].
        /// </summary>
        /// <typeparam name="T">Comparable type.</typeparam>
        /// <param name="value">Value to validate.</param>
        /// <param name="min">Minimum allowed value.</param>
        /// <param name="max">Maximum allowed value.</param>
        /// <param name="parameterName">Parameter name.</param>
        /// <exception cref="ArgumentException">Thrown when the value is out of range.</exception>
        public static void OutOfRange<T>(T value, T min, T max, string parameterName)
            where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            {
                throw new ArgumentException(
                    $"Parameter '{parameterName}' must be between '{min}' and '{max}'.",
                    parameterName);
            }
        }

        #endregion
    }
}

