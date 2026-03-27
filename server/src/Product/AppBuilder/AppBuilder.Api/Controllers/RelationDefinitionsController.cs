using AppBuilder.Application.Commands.CreateRelationDefinition;
using AppBuilder.Application.Commands.DeleteRelationDefinition;
using AppBuilder.Application.Commands.UpdateRelationDefinition;
using AppBuilder.Application.Queries.GetRelationDefinition;
using AppBuilder.Application.Queries.ListRelationsBySourceEntity;
using AppBuilder.Application.Queries.ListRelationsByTargetEntity;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for relation definitions (AppBuilder module).</summary>
[ApiController]
[Route("api/appbuilder")]
public sealed class RelationDefinitionsController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public RelationDefinitionsController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>Create relation between entities.</summary>
    [HttpPost("relations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateRelationRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new CreateRelationDefinitionCommand(request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetRelationDefinitionQuery(result.Value!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, getResult.Value);
    }

    /// <summary>Get relation by ID.</summary>
    [HttpGet("relations/{id:guid}")]
    [ProducesResponseType(typeof(RelationDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetRelationDefinitionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>List relations by source entity (outgoing).</summary>
    [HttpGet("entities/{entityId:guid}/relations")]
    [ProducesResponseType(typeof(List<RelationDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBySourceEntity(Guid entityId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListRelationsBySourceEntityQuery(entityId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>List relations by target entity (incoming).</summary>
    [HttpGet("entities/{entityId:guid}/incoming-relations")]
    [ProducesResponseType(typeof(List<RelationDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByTargetEntity(Guid entityId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListRelationsByTargetEntityQuery(entityId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Update relation (cascade delete flag).</summary>
    [HttpPut("relations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRelationRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new UpdateRelationDefinitionCommand(id, request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetRelationDefinitionQuery(id), cancellationToken);
        return Ok(getResult.Value);
    }

    /// <summary>Delete relation.</summary>
    [HttpDelete("relations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeleteRelationDefinitionCommand(id), cancellationToken);
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
