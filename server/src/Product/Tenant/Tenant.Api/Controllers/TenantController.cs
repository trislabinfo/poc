using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenant.Api.Requests;
using Tenant.Application.Commands.CreateTenant;
using Tenant.Application.Commands.CreateTenantWithUsers;
using Tenant.Application.DTOs;
using Tenant.Application.Queries.GetTenantById;
using Tenant.Contracts.Services;
using TenantEntity = Tenant.Domain.Entities.Tenant;

namespace Tenant.Api.Controllers;

/// <summary>
/// Tenant CRUD API via <see cref="BaseCrudController{TEntity,TId,TCreateRequest,TUpdateRequest,TResponse}"/>.
/// GetById and Create are implemented; Update and Delete return validation error until implemented.
/// </summary>
[ApiController]
[Route("api/tenant")]
public sealed class TenantController : BaseCrudController<TenantEntity, Guid, CreateTenantRequest, UpdateTenantRequest, TenantDto>
{
    private readonly ITenantResolverService _tenantResolver;

    public TenantController(IRequestDispatcher requestDispatcher, ITenantResolverService tenantResolver)
        : base(requestDispatcher)
    {
        _tenantResolver = tenantResolver;
    }

    /// <inheritdoc />
    protected override Guid GetIdFromResponse(TenantDto response) => response.Id;

    /// <inheritdoc />
    protected override async Task<Result<TenantDto>> HandleGetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await RequestDispatcher.SendAsync(new GetTenantByIdQuery(id), cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<Result<TenantDto>> HandleCreateAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var createResult = await RequestDispatcher.SendAsync(
            new CreateTenantCommand(request.Name, request.Slug),
            cancellationToken);

        if (createResult.IsFailure)
            return Result<TenantDto>.Failure(createResult.Error);

        return await RequestDispatcher.SendAsync(new GetTenantByIdQuery(createResult.Value), cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<Result<TenantDto>> HandleUpdateAsync(
        Guid id,
        UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<TenantDto>.Failure(
            Error.Validation("Tenant.UpdateNotImplemented", "Update is not implemented.")));
    }

    /// <inheritdoc />
    protected override Task<Result> HandleDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Failure(
            Error.Validation("Tenant.DeleteNotImplemented", "Delete is not implemented.")));
    }

    /// <summary>
    /// Gets tenant info by slug (for cross-module resolution, e.g. TenantApplication).
    /// </summary>
    [HttpGet("by-slug/{slug}")]
    [ProducesResponseType(typeof(Tenant.Contracts.TenantInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _tenantResolver.GetBySlugAsync(slug, cancellationToken);
        if (result.IsFailure)
            return ToActionResult(Result.Failure(result.Error));
        if (result.Value == null)
            return NotFound();
        return Ok(result.Value);
    }

    /// <summary>
    /// Gets tenant info by id (for cross-module resolution, e.g. TenantApplication).
    /// </summary>
    [HttpGet("{id:guid}/info")]
    [ProducesResponseType(typeof(Tenant.Contracts.TenantInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInfoById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _tenantResolver.GetByIdAsync(id, cancellationToken);
        if (result.IsFailure)
            return ToActionResult(Result.Failure(result.Error));
        if (result.Value == null)
            return NotFound();
        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a tenant with one or more users. Each user is also created in Identity for authentication.
    /// </summary>
    [HttpPost("with-users")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWithUsers(
        [FromBody] CreateTenantWithUsersRequest request,
        CancellationToken cancellationToken)
    {
        var users = request.Users
            .Select(u => new UserData(u.Email, u.DisplayName, u.Password, u.IsTenantOwner))
            .ToList();
        var command = new CreateTenantWithUsersCommand(request.Name, request.Slug, users);
        var result = await RequestDispatcher.SendAsync(command, cancellationToken);
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
        return ToActionResult(result);
    }
}
