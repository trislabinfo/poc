using AppBuilder.Api.Requests;
using AppBuilder.Application.Commands.CreateAppDefinition;
using AppBuilder.Application.Commands.DeleteAppDefinition;
using AppBuilder.Application.Commands.UpdateAppDefinition;
using AppBuilder.Application.DTOs;
using AppBuilder.Application.Queries.GetAppDefinitionById;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>
/// REST API for application definitions (AppBuilder module). Create and get by id implemented; update/delete return validation until implemented.
/// </summary>
[ApiController]
[Route("api/appbuilder/application-definitions")]
public sealed class AppDefinitionsController : BaseCrudController<AppDefinition.Domain.Entities.Application.AppDefinition, Guid, CreateAppDefinitionRequest, UpdateAppDefinitionRequest, AppDefinitionDto>
{
    public AppDefinitionsController(IRequestDispatcher requestDispatcher)
        : base(requestDispatcher)
    {
    }

    /// <inheritdoc />
    protected override Guid GetIdFromResponse(AppDefinitionDto response) => response.Id;

    /// <inheritdoc />
    protected override async Task<Result<AppDefinitionDto>> HandleGetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await RequestDispatcher.SendAsync(new GetAppDefinitionByIdQuery(id), cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<Result<AppDefinitionDto>> HandleCreateAsync(
        CreateAppDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var createResult = await RequestDispatcher.SendAsync(
            new CreateAppDefinitionCommand(
                request.Name,
                request.Description,
                request.Slug,
                request.IsPublic),
            cancellationToken);

        if (createResult.IsFailure)
            return Result<AppDefinitionDto>.Failure(createResult.Error);

        return await RequestDispatcher.SendAsync(new GetAppDefinitionByIdQuery(createResult.Value), cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<Result<AppDefinitionDto>> HandleUpdateAsync(
        Guid id,
        UpdateAppDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await RequestDispatcher.SendAsync<Result>(new UpdateAppDefinitionCommand(id, request.Name, request.Description), cancellationToken);
        if (result.IsFailure) return Result<AppDefinitionDto>.Failure(result.Error);
        return await RequestDispatcher.SendAsync<Result<AppDefinitionDto>>(new GetAppDefinitionByIdQuery(id), cancellationToken);
    }

    /// <inheritdoc />
    protected override async Task<Result> HandleDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return await RequestDispatcher.SendAsync<Result>(new DeleteAppDefinitionCommand(id), cancellationToken);
    }
}
