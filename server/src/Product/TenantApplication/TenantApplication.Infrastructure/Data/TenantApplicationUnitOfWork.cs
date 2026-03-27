using BuildingBlocks.Infrastructure.Persistence;
using TenantApplication.Application;

namespace TenantApplication.Infrastructure.Data;

public sealed class TenantApplicationUnitOfWork : UnitOfWork<TenantApplicationDbContext>, ITenantApplicationUnitOfWork
{
    public TenantApplicationUnitOfWork(TenantApplicationDbContext context)
        : base(context)
    {
    }
}
