using BuildingBlocks.Infrastructure.Persistence;
using Tenant.Application;

namespace Tenant.Infrastructure.Data;

/// <summary>
/// Unit of work for TenantDbContext. Used by <see cref="Tenant.Application.Behaviors.TenantTransactionBehavior"/>.
/// </summary>
public sealed class TenantUnitOfWork : UnitOfWork<TenantDbContext>, ITenantUnitOfWork
{
    public TenantUnitOfWork(TenantDbContext context)
        : base(context)
    {
    }
}
