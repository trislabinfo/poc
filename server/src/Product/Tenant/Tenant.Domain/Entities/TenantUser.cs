using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;

namespace Tenant.Domain.Entities;

/// <summary>
/// Represents a user associated with a tenant (application-level link to Identity.User via UserId).
/// </summary>
public sealed class TenantUser : Entity<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsTenantOwner { get; private set; }

    private TenantUser()
    {
        // EF Core
    }

    private TenantUser(Guid tenantId, Guid userId, bool isTenantOwner, IDateTimeProvider dateTimeProvider)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        IsTenantOwner = isTenantOwner;
        CreatedAt = dateTimeProvider.UtcNow;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    public static Result<TenantUser> Create(
        Guid tenantId,
        Guid userId,
        bool isTenantOwner,
        IDateTimeProvider dateTimeProvider)
    {
        if (tenantId == Guid.Empty)
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.TenantId", "Tenant ID is required."));
        if (userId == Guid.Empty)
            return Result<TenantUser>.Failure(Error.Validation("TenantUser.UserId", "User ID is required."));

        var dateTimeProviderResult = Guard.Against.Null(dateTimeProvider, nameof(dateTimeProvider));
        if (dateTimeProviderResult.IsFailure)
            return Result<TenantUser>.Failure(dateTimeProviderResult.Error);

        return Result<TenantUser>.Success(new TenantUser(tenantId, userId, isTenantOwner, dateTimeProvider));
    }
}
