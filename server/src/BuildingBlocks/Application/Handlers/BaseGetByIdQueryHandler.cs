using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Persistence;
using BuildingBlocks.Kernel.Results;

namespace BuildingBlocks.Application.Handlers;

/// <summary>
/// Base handler for get-by-id queries: load entity, map to response, or return NotFound.
/// </summary>
/// <typeparam name="TEntity">Entity type.</typeparam>
/// <typeparam name="TKey">Entity id type.</typeparam>
/// <typeparam name="TResponse">Response DTO type.</typeparam>
/// <typeparam name="TQuery">Query type (must implement IApplicationRequest&lt;Result&lt;TResponse&gt;&gt;).</typeparam>
public abstract class BaseGetByIdQueryHandler<TEntity, TKey, TResponse, TQuery> : IApplicationRequestHandler<TQuery, Result<TResponse>>
    where TEntity : Entity<TKey>
    where TKey : notnull
    where TQuery : IApplicationRequest<Result<TResponse>>
{
    /// <summary>Repository for the entity.</summary>
    protected IRepository<TEntity, TKey> Repository { get; }

    /// <summary>Initializes the handler with repository.</summary>
    protected BaseGetByIdQueryHandler(IRepository<TEntity, TKey> repository)
    {
        Repository = repository;
    }

    /// <inheritdoc />
    public async Task<Result<TResponse>> HandleAsync(TQuery request, CancellationToken cancellationToken)
    {
        var id = GetIdFromQuery(request);
        var entity = await Repository.GetByIdAsync(id, cancellationToken);
        if (entity is null)
            return Result<TResponse>.Failure(Error.NotFound(NotFoundCode, NotFoundMessage));

        return Result<TResponse>.Success(MapToResponse(entity));
    }

    /// <summary>Error code for not-found (e.g. "Tenant.NotFound"). Override in derived class.</summary>
    protected virtual string NotFoundCode => $"{typeof(TEntity).Name}.NotFound";

    /// <summary>Error message for not-found. Override in derived class.</summary>
    protected virtual string NotFoundMessage => $"{typeof(TEntity).Name} not found.";

    /// <summary>Extracts the entity id from the query (e.g. query.TenantId or query.Id).</summary>
    protected abstract TKey GetIdFromQuery(TQuery query);

    /// <summary>Maps the entity to the response DTO.</summary>
    protected abstract TResponse MapToResponse(TEntity entity);
}
