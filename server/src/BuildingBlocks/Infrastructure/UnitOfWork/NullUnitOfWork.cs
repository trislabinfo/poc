using BuildingBlocks.Application.UnitOfWork;

namespace BuildingBlocks.Infrastructure.UnitOfWork;

/// <summary>
/// No-op unit of work when no database transaction is configured.
/// Modules that need transactions should register an EF-based IUnitOfWork (e.g. EfUnitOfWork&lt;TContext&gt;) instead.
/// </summary>
internal sealed class NullUnitOfWork : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task CommitTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
