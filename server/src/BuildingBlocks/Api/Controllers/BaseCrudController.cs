using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Web.Controllers;

/// <summary>
/// Base controller providing standard CRUD endpoints. Derive and implement the Handle*Async methods.
/// </summary>
/// <typeparam name="TEntity">Entity type (must inherit Entity{TId}).</typeparam>
/// <typeparam name="TId">Entity identifier type.</typeparam>
/// <typeparam name="TCreateRequest">Request DTO for create.</typeparam>
/// <typeparam name="TUpdateRequest">Request DTO for update.</typeparam>
/// <typeparam name="TResponse">Response DTO (e.g. with Id for 201 Location).</typeparam>
public abstract class BaseCrudController<TEntity, TId, TCreateRequest, TUpdateRequest, TResponse>
    : BaseApiController
    where TEntity : Entity<TId>
    where TId : notnull
{
    /// <inheritdoc />
    protected BaseCrudController(IRequestDispatcher requestDispatcher)
        : base(requestDispatcher)
    {
    }

    /// <summary>
    /// Returns the identifier from a response (used for 201 Created Location header).
    /// </summary>
    protected abstract TId GetIdFromResponse(TResponse response);

    /// <summary>
    /// GET paged list filtered by specification. Override <see cref="HandleGetByPagingAsync"/> to provide implementation.
    /// Default implementation throws <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 OK with <see cref="PagedResponse{T}"/> of <typeparamref name="TResponse"/>.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public virtual async Task<IActionResult> GetByPaging(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await HandleGetByPagingAsync(page, pageSize, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Load a paged list of items filtered by specification. Default throws; override to return pagination data.
    /// </summary>
    protected virtual Task<Result<PagedResponse<TResponse>>> HandleGetByPagingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException(
            "GetByPaging is not implemented. Override HandleGetByPagingAsync to return pagination data filtered by specification.");
    }

    /// <summary>
    /// GET by id. Override <see cref="HandleGetByIdAsync"/> to provide implementation.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> GetById(TId id, CancellationToken cancellationToken = default)
    {
        var result = await HandleGetByIdAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// POST create. On success returns 201 Created with Location header. Override <see cref="HandleCreateAsync"/>.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public virtual async Task<IActionResult> Create(
        [FromBody] TCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await HandleCreateAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            var id = GetIdFromResponse(result.Value);
            return CreatedAtGetById(nameof(GetById), new { id }, result.Value);
        }
        return ToActionResult(result);
    }

    /// <summary>
    /// PUT update. Override <see cref="HandleUpdateAsync"/> to provide implementation.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Update(
        TId id,
        [FromBody] TUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await HandleUpdateAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// DELETE. Override <see cref="HandleDeleteAsync"/> to provide implementation. Returns 200 OK with <see cref="ApiResponse"/> envelope on success.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public virtual async Task<IActionResult> Delete(TId id, CancellationToken cancellationToken = default)
    {
        var result = await HandleDeleteAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>
    /// Load a single entity by id and map to response. Return NotFound when not found.
    /// </summary>
    protected abstract Task<Result<TResponse>> HandleGetByIdAsync(TId id, CancellationToken cancellationToken);

    /// <summary>
    /// Create entity from request and return the response (or validation/conflict error).
    /// </summary>
    protected abstract Task<Result<TResponse>> HandleCreateAsync(
        TCreateRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Update entity by id. Return NotFound when not found.
    /// </summary>
    protected abstract Task<Result<TResponse>> HandleUpdateAsync(
        TId id,
        TUpdateRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Delete entity by id. Return NotFound when not found.
    /// </summary>
    protected abstract Task<Result> HandleDeleteAsync(TId id, CancellationToken cancellationToken);
}
