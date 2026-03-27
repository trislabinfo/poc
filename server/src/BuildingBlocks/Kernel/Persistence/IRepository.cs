using Ardalis.Specification;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using System.Linq.Expressions;

namespace BuildingBlocks.Kernel.Persistence;

/// <summary>
/// Repository abstraction for entity persistence. Implemented by Infrastructure.
/// </summary>
public interface IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : notnull
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FirstOrDefaultAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<TEntity>> GetByPaginationAsync(
        ISpecification<TEntity> specification,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        ISpecification<TEntity> specification,
        CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Delete(TEntity entity);
}
