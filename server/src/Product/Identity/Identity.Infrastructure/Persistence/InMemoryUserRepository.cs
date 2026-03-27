using Ardalis.Specification;
using BuildingBlocks.Kernel.Results;
using Identity.Domain.Entities;
using Identity.Domain.Repositories;
using Identity.Domain.ValueObjects;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Identity.Infrastructure.Persistence;

/// <summary>
/// In-memory implementation of <see cref="IUserRepository"/> for development and testing.
/// Replace with a database-backed implementation (e.g. EF Core) for production.
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _store = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.TryGetValue(id, out var user) ? user : null);
    }

    public Task<User?> FirstOrDefaultAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var user = _store.Values.AsQueryable().FirstOrDefault(predicate);
        return Task.FromResult(user);
    }

    public Task<User?> FirstOrDefaultAsync(
        ISpecification<User> specification,
        CancellationToken cancellationToken = default)
    {
        var result = EvaluateSpec(specification).FirstOrDefault();
        return Task.FromResult(result);
    }

    public Task<PagedResponse<User>> GetByPaginationAsync(
        ISpecification<User> specification,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var all = EvaluateSpec(specification).ToList();
        var totalCount = all.Count;
        var pageNumber = Math.Max(1, page);
        var size = Math.Max(1, Math.Min(pageSize, 100));
        var items = all
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToList();
        var paged = new PagedResponse<User>(items, pageNumber, size, totalCount);
        return Task.FromResult(paged);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Count);
    }

    public Task<int> CountAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var count = _store.Values.AsQueryable().Count(predicate);
        return Task.FromResult(count);
    }

    public Task<int> CountAsync(
        ISpecification<User> specification,
        CancellationToken cancellationToken = default)
    {
        var count = EvaluateSpec(specification).Count();
        return Task.FromResult(count);
    }

    public Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        _store[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public void Update(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        _store[entity.Id] = entity;
    }

    public void Delete(User entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));
        _store.TryRemove(entity.Id, out _);
    }

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var user = _store.Values.FirstOrDefault(u => u.Email.Value == email.Value);
        return Task.FromResult(user);
    }

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        var exists = _store.Values.Any(u => u.Email.Value == email.Value);
        return Task.FromResult(exists);
    }

    private IEnumerable<User> EvaluateSpec(ISpecification<User> specification)
    {
        // Ardalis.Specification: Specification<T> has Evaluate(IEnumerable<T>) for in-memory
        if (specification is Specification<User> spec)
            return spec.Evaluate(_store.Values);
        // Fallback: apply as expression if possible (single-criteria specs)
        return _store.Values.AsEnumerable();
    }
}
