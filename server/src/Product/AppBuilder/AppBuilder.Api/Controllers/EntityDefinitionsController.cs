using AppBuilder.Application.Commands.CreateEntityDefinition;
using AppBuilder.Application.Commands.DeleteEntityDefinition;
using AppBuilder.Application.Commands.UpdateEntityDefinition;
using AppBuilder.Application.Queries.GetEntityDefinition;
using AppBuilder.Application.Queries.ListEntitiesByApplication;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for entity definitions (AppBuilder module).</summary>
[ApiController]
[Route("api/appbuilder")]
public sealed class EntityDefinitionsController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public EntityDefinitionsController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>Create entity.</summary>
    [HttpPost("entities")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEntityRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new CreateEntityDefinitionCommand(request), cancellationToken);
        if (result.IsFailure) return ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetEntityDefinitionQuery(result.Value!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, getResult.Value);
    }

    /// <summary>Get entity by ID.</summary>
    [HttpGet("entities/{id:guid}")]
    [ProducesResponseType(typeof(EntityDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetEntityDefinitionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>List entities by application.</summary>
    [HttpGet("applications/{applicationId:guid}/entities")]
    [ProducesResponseType(typeof(List<EntityDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListEntitiesByApplicationQuery(applicationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Update entity.</summary>
    [HttpPut("entities/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEntityRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new UpdateEntityDefinitionCommand(id, request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetEntityDefinitionQuery(id), cancellationToken);
        return Ok(getResult.Value);
    }

    /// <summary>Delete entity.</summary>
    [HttpDelete("entities/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeleteEntityDefinitionCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
    }

    private IActionResult ResultToProblem(Result result) =>
        Problem(detail: result.Error.Message, statusCode: result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        });

    private IActionResult ResultToProblem<T>(Result<T> result) =>
        Problem(detail: result.Error.Message, statusCode: result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        });
}
