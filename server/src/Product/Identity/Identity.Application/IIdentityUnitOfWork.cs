using BuildingBlocks.Application.UnitOfWork;

namespace Identity.Application;

/// <summary>
/// Unit of work for the Identity module's DbContext. Used by <see cref="Behaviors.IdentityTransactionBehavior"/>.
/// </summary>
public interface IIdentityUnitOfWork : IUnitOfWork
{
}
