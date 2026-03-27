using BuildingBlocks.Infrastructure.Persistence;
using Identity.Application;

namespace Identity.Infrastructure.Data;

/// <summary>
/// Unit of work for IdentityDbContext. Used by <see cref="Identity.Application.Behaviors.IdentityTransactionBehavior"/>.
/// </summary>
public sealed class IdentityUnitOfWork : UnitOfWork<IdentityDbContext>, IIdentityUnitOfWork
{
    public IdentityUnitOfWork(IdentityDbContext context)
        : base(context)
    {
    }
}
