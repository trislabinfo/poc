using AppBuilder.Application.Commands.CreatePropertyDefinition;
using AppBuilder.Application.Commands.DeletePropertyDefinition;
using AppBuilder.Application.Commands.UpdatePropertyDefinition;
using AppBuilder.Application.Queries.GetPropertyDefinition;
using AppBuilder.Application.Queries.ListPropertiesByEntity;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for property definitions (AppBuilder module).</summary>
[ApiController]
[Route("api/appbuilder")]
public sealed class PropertyDefinitionsController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public PropertyDefinitionsController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>Create property for an entity.</summary>
    [HttpPost("entities/{entityId:guid}/properties")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid entityId, [FromBody] CreatePropertyRequest request, CancellationToken cancellationToken)
    {
        var req = request with { EntityDefinitionId = entityId };
        var result = await _requestDispatcher.SendAsync(new CreatePropertyDefinitionCommand(req), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == ErrorType.NotFound ? NotFound() : result.Error.Type == ErrorType.Conflict ? Conflict() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetPropertyDefinitionQuery(result.Value!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, getResult.Value);
    }

    /// <summary>Get property by ID.</summary>
    [HttpGet("properties/{id:guid}")]
    [ProducesResponseType(typeof(PropertyDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetPropertyDefinitionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>List properties by entity.</summary>
    [HttpGet("entities/{entityId:guid}/properties")]
    [ProducesResponseType(typeof(List<PropertyDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByEntity(Guid entityId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListPropertiesByEntityQuery(entityId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Update property.</summary>
    [HttpPut("properties/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new UpdatePropertyDefinitionCommand(id, request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetPropertyDefinitionQuery(id), cancellationToken);
        return Ok(getResult.Value);
    }

    /// <summary>Delete property.</summary>
    [HttpDelete("properties/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeletePropertyDefinitionCommand(id), cancellationToken);
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
