using BuildingBlocks.Application.UnitOfWork;

namespace Tenant.Application;

/// <summary>
/// Unit of work for the Tenant module's DbContext. Used by <see cref="Behaviors.TenantTransactionBehavior"/>.
/// </summary>
public interface ITenantUnitOfWork : IUnitOfWork
{
}
