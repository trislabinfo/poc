using AppBuilder.Api.Requests;
using AppBuilder.Application.Commands.CreateAppDefinition;
using AppBuilder.Application.Commands.DeleteAppDefinition;
using AppBuilder.Application.Commands.UpdateAppDefinition;
using AppBuilder.Application.DTOs;
using AppBuilder.Application.Queries.GetAppDefinitionById;
using AppBuilder.Application.Queries.GetInstallableApplications;
using AppBuilder.Application.Queries.ListAppDefinitions;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for platform applications (alias route api/appbuilder/applications). List, installable catalog, CRUD.</summary>
[ApiController]
[Route("api/appbuilder/applications")]
public sealed class ApplicationsController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public ApplicationsController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>List applications, optionally filtered by status or isPublic.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<AppDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] bool? isPublic, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListAppDefinitionsQuery(status, isPublic), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Get applications available for tenant install (public and with active release).</summary>
    [HttpGet("installable")]
    [ProducesResponseType(typeof(List<InstallableApplicationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInstallable(CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetInstallableApplicationsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Get application by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetAppDefinitionByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
    }

    /// <summary>Create application (draft).</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateAppDefinitionRequest request, CancellationToken cancellationToken)
    {
        var createResult = await _requestDispatcher.SendAsync(
            new CreateAppDefinitionCommand(request.Name, request.Description, request.Slug, request.IsPublic),
            cancellationToken);
        if (createResult.IsFailure)
            return createResult.Error.Type == ErrorType.Conflict ? Conflict() : ResultToProblem(createResult);
        var getResult = await _requestDispatcher.SendAsync(new GetAppDefinitionByIdQuery(createResult.Value!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = createResult.Value }, getResult.Value);
    }

    /// <summary>Update application (draft only).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AppDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppDefinitionRequest request, CancellationToken cancellationToken)
    {
        var updateResult = await _requestDispatcher.SendAsync(new UpdateAppDefinitionCommand(id, request.Name, request.Description), cancellationToken);
        if (updateResult.IsFailure)
            return updateResult.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(updateResult);
        var getResult = await _requestDispatcher.SendAsync(new GetAppDefinitionByIdQuery(id), cancellationToken);
        return Ok(getResult.Value);
    }

    /// <summary>Delete application (draft only).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeleteAppDefinitionCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
    }

    private IActionResult ResultToProblem(Result result) =>
        Problem(detail: result.Error.Message, statusCode: result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        });

    private IActionResult ResultToProblem<T>(Result<T> result) =>
        Problem(detail: result.Error.Message, statusCode: result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        });
}
