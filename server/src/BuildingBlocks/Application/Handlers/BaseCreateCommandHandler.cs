using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Application.UnitOfWork;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Persistence;
using BuildingBlocks.Kernel.Results;

namespace BuildingBlocks.Application.Handlers;

/// <summary>
/// Base handler for create commands: create entity (via abstract method), add to repository, return id.
/// Relies on the module's transaction behavior (e.g. TenantTransactionBehavior) to commit the unit of work.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TKey">Entity id type.</typeparam>
/// <typeparam name="TCommand">Command type (must implement IApplicationRequest&lt;Result&lt;TKey&gt;&gt;).</typeparam>
public abstract class BaseCreateCommandHandler<TEntity, TKey, TCommand> : IApplicationRequestHandler<TCommand, Result<TKey>>
    where TEntity : Entity<TKey>
    where TKey : notnull
    where TCommand : IApplicationRequest<Result<TKey>>
{
    /// <summary>Repository for the entity.</summary>
    protected IRepository<TEntity, TKey> Repository { get; }

    /// <summary>Unit of work (commit is typically done by the module's transaction behavior).</summary>
    protected IUnitOfWork UnitOfWork { get; }

    /// <summary>Initializes the handler with repository and unit of work.</summary>
    protected BaseCreateCommandHandler(IRepository<TEntity, TKey> repository, IUnitOfWork unitOfWork)
    {
        Repository = repository;
        UnitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TKey>> HandleAsync(TCommand request, CancellationToken cancellationToken)
    {
        var entityResult = await CreateEntityAsync(request, cancellationToken);
        if (entityResult.IsFailure)
            return Result<TKey>.Failure(entityResult.Error);

        var entity = entityResult.Value;
        await Repository.AddAsync(entity, cancellationToken);
        return Result<TKey>.Success(entity.Id);
    }

    /// <summary>
    /// Creates the entity from the command. Implement in derived handlers (e.g. call domain factory).
    /// </summary>
    protected abstract Task<Result<TEntity>> CreateEntityAsync(TCommand command, CancellationToken cancellationToken);
}
