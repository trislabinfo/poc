using AppBuilder.Application.Commands.CreateNavigationDefinition;
using AppBuilder.Application.Commands.DeleteNavigationDefinition;
using AppBuilder.Application.Commands.UpdateNavigationDefinition;
using AppBuilder.Application.Queries.GetNavigationDefinition;
using AppBuilder.Application.Queries.ListNavigationsByApplication;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for navigation definitions (AppBuilder module).</summary>
[ApiController]
[Route("api/appbuilder")]
public sealed class NavigationDefinitionsController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public NavigationDefinitionsController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>Create navigation for an application.</summary>
    [HttpPost("navigations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateNavigationRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new CreateNavigationDefinitionCommand(request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetNavigationDefinitionQuery(result.Value!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, getResult.Value);
    }

    /// <summary>Get navigation by ID.</summary>
    [HttpGet("navigations/{id:guid}")]
    [ProducesResponseType(typeof(NavigationDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetNavigationDefinitionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>List navigations by application.</summary>
    [HttpGet("applications/{applicationId:guid}/navigations")]
    [ProducesResponseType(typeof(List<NavigationDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListNavigationsByApplicationQuery(applicationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Update navigation.</summary>
    [HttpPut("navigations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNavigationRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new UpdateNavigationDefinitionCommand(id, request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetNavigationDefinitionQuery(id), cancellationToken);
        return Ok(getResult.Value);
    }

    /// <summary>Delete navigation.</summary>
    [HttpDelete("navigations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeleteNavigationDefinitionCommand(id), cancellationToken);
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
