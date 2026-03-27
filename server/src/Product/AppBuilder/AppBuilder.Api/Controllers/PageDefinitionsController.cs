using AppBuilder.Application.Commands.CreatePageDefinition;
using AppBuilder.Application.Commands.DeletePageDefinition;
using AppBuilder.Application.Commands.UpdatePageDefinition;
using AppBuilder.Application.Queries.GetPageDefinition;
using AppBuilder.Application.Queries.ListPagesByApplication;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for page definitions (AppBuilder module).</summary>
[ApiController]
[Route("api/appbuilder")]
public sealed class PageDefinitionsController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public PageDefinitionsController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>Create page for an application.</summary>
    [HttpPost("pages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new CreatePageDefinitionCommand(request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetPageDefinitionQuery(result.Value!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, getResult.Value);
    }

    /// <summary>Get page by ID.</summary>
    [HttpGet("pages/{id:guid}")]
    [ProducesResponseType(typeof(PageDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetPageDefinitionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>List pages by application.</summary>
    [HttpGet("applications/{applicationId:guid}/pages")]
    [ProducesResponseType(typeof(List<PageDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListPagesByApplicationQuery(applicationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Update page.</summary>
    [HttpPut("pages/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePageRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new UpdatePageDefinitionCommand(id, request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetPageDefinitionQuery(id), cancellationToken);
        return Ok(getResult.Value);
    }

    /// <summary>Delete page.</summary>
    [HttpDelete("pages/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeletePageDefinitionCommand(id), cancellationToken);
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
