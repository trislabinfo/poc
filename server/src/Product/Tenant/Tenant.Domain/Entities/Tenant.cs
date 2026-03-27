using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using System.Text.RegularExpressions;
using Tenant.Domain.Events;

namespace Tenant.Domain.Entities;

/// <summary>
/// Tenant aggregate root. Represents a tenant (organization) with name and slug.
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>
{
    private static readonly Regex SlugRegex = new(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly List<TenantUser> _tenantUsers = [];

    /// <summary>Display name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>URL-friendly identifier (unique).</summary>
    public string Slug { get; private set; } = string.Empty;
    /// <summary>Tenant users (link to Identity users).</summary>
    public IReadOnlyCollection<TenantUser> Users => _tenantUsers.AsReadOnly();

    private Tenant()
    {
        // For EF Core
    }

    /// <summary>
    /// Creates a new tenant.
    /// </summary>
    /// <param name="name">Display name (1–200 characters).</param>
    /// <param name="slug">URL-friendly slug (lowercase, [a-z0-9-], 1–100 characters).</param>
    /// <param name="dateTimeProvider">Date/time provider for timestamps.</param>
    /// <returns>Result containing the created tenant or a validation error.</returns>
    public static Result<Tenant> Create(
        string name,
        string slug,
        IDateTimeProvider dateTimeProvider)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure)
            return Result<Tenant>.Failure(nameResult.Error);

        var slugResult = Guard.Against.NullOrWhiteSpace(slug, nameof(slug));
        if (slugResult.IsFailure)
            return Result<Tenant>.Failure(slugResult.Error);

        var normalizedSlug = slug.Trim().ToLowerInvariant();
        if (normalizedSlug.Length > 100)
        {
            return Result<Tenant>.Failure(
                Error.Validation("Tenant.SlugTooLong", "Slug cannot exceed 100 characters."));
        }

        if (!SlugRegex.IsMatch(normalizedSlug))
        {
            return Result<Tenant>.Failure(
                Error.Validation("Tenant.InvalidSlugFormat", "Slug must contain only lowercase letters, numbers, and hyphens (e.g. my-tenant)."));
        }

        if (name.Trim().Length > 200)
        {
            return Result<Tenant>.Failure(
                Error.Validation("Tenant.NameTooLong", "Name cannot exceed 200 characters."));
        }

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<Tenant>.Failure(dateTimeProviderResult.Error);

        var now = dateTimeProvider.UtcNow;

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = normalizedSlug,
            CreatedAt = now
        };

        tenant.RaiseDomainEvent(new TenantCreatedEvent(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            now));

        return Result<Tenant>.Success(tenant);
    }

    /// <summary>
    /// Updates the tenant name.
    /// </summary>
    public Result UpdateName(string name, IDateTimeProvider dateTimeProvider)
    {
        var nameResult = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        if (nameResult.IsFailure)
            return nameResult;

        if (name.Trim().Length > 200)
        {
            return Result.Failure(
                Error.Validation("Tenant.NameTooLong", "Name cannot exceed 200 characters."));
        }

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return dateTimeProviderResult;

        Name = name.Trim();
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Adds a user to this tenant (after the user has been created in Identity).
    /// </summary>
    public Result AddUser(Guid userId, bool isTenantOwner, IDateTimeProvider dateTimeProvider)
    {
        if (_tenantUsers.Any(u => u.UserId == userId))
            return Result.Failure(Error.Validation("Tenant.Users", "User is already associated with this tenant."));

        var userResult = TenantUser.Create(Id, userId, isTenantOwner, dateTimeProvider);
        if (userResult.IsFailure)
            return Result.Failure(userResult.Error);

        _tenantUsers.Add(userResult.Value);
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Removes a user from this tenant (e.g. for rollback).
    /// </summary>
    public Result RemoveUser(Guid userId, IDateTimeProvider dateTimeProvider)
    {
        var user = _tenantUsers.FirstOrDefault(u => u.UserId == userId);
        if (user is null)
            return Result.Failure(Error.NotFound("Tenant.User", "User not found in tenant."));
        if (user.IsTenantOwner && _tenantUsers.Count(u => u.IsTenantOwner) == 1)
            return Result.Failure(Error.Validation("Tenant.Users", "Cannot remove the last tenant owner."));

        _tenantUsers.Remove(user);
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }
}
