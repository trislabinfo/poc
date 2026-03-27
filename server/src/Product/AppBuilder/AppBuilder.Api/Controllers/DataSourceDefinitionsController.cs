using AppBuilder.Application.Commands.CreateDataSourceDefinition;
using AppBuilder.Application.Commands.DeleteDataSourceDefinition;
using AppBuilder.Application.Commands.UpdateDataSourceDefinition;
using AppBuilder.Application.Queries.GetDataSourceDefinition;
using AppBuilder.Application.Queries.ListDataSourcesByApplication;
using AppDefinition.Contracts.DTOs;
using AppDefinition.Contracts.Requests;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for data source definitions (AppBuilder module).</summary>
[ApiController]
[Route("api/appbuilder")]
public sealed class DataSourceDefinitionsController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public DataSourceDefinitionsController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>Create data source for an application.</summary>
    [HttpPost("data-sources")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateDataSourceRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new CreateDataSourceDefinitionCommand(request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetDataSourceDefinitionQuery(result.Value!), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Value }, getResult.Value);
    }

    /// <summary>Get data source by ID.</summary>
    [HttpGet("data-sources/{id:guid}")]
    [ProducesResponseType(typeof(DataSourceDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetDataSourceDefinitionQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>List data sources by application.</summary>
    [HttpGet("applications/{applicationId:guid}/data-sources")]
    [ProducesResponseType(typeof(List<DataSourceDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListDataSourcesByApplicationQuery(applicationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ResultToProblem(result);
    }

    /// <summary>Update data source.</summary>
    [HttpPut("data-sources/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDataSourceRequest request, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new UpdateDataSourceDefinitionCommand(id, request), cancellationToken);
        if (result.IsFailure) return result.Error.Type == ErrorType.NotFound ? NotFound() : ResultToProblem(result);
        var getResult = await _requestDispatcher.SendAsync(new GetDataSourceDefinitionQuery(id), cancellationToken);
        return Ok(getResult.Value);
    }

    /// <summary>Delete data source.</summary>
    [HttpDelete("data-sources/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeleteDataSourceDefinitionCommand(id), cancellationToken);
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
