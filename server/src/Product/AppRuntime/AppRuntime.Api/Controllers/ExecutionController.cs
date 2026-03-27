using AppRuntime.Contracts.DTOs;
using AppRuntime.Contracts.Services;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppRuntime.Api.Controllers;

/// <summary>
/// Runtime execution endpoints (datasource, engines). For single-host MVP, BFF may call these in-process; for multi-host, BFF forwards here.
/// </summary>
[ApiController]
[Route("api/appruntime")]
public sealed class ExecutionController : ControllerBase
{
    private readonly IDatasourceExecutionService _datasourceExecutionService;

    public ExecutionController(IDatasourceExecutionService datasourceExecutionService)
    {
        _datasourceExecutionService = datasourceExecutionService;
    }

    /// <summary>
    /// Execute a datasource for the given application release.
    /// </summary>
    [HttpPost("datasource/execute")]
    [ProducesResponseType(typeof(ApiResponse<DatasourceExecuteResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteDatasource(
        [FromBody] DatasourceExecuteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || request.ApplicationReleaseId == Guid.Empty)
            return BadRequest(ApiResponse.CreateFailure(new ErrorResponse("Validation", "applicationReleaseId is required.")));
        if (string.IsNullOrWhiteSpace(request.DatasourceId))
            return BadRequest(ApiResponse.CreateFailure(new ErrorResponse("Validation", "datasourceId is required.")));

        var result = await _datasourceExecutionService.ExecuteAsync(
            request.ApplicationReleaseId,
            request.DatasourceId,
            cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<DatasourceExecuteResultDto>.CreateSuccess(result.Value));

        var errorResponse = new ErrorResponse(
            result.Error.Code,
            result.Error.Message,
            HttpContext.Items["CorrelationId"]?.ToString(),
            DateTime.UtcNow);

        return result.Error.Type switch
        {
            ErrorType.NotFound => NotFound(ApiResponse.CreateFailure(errorResponse)),
            ErrorType.Validation => BadRequest(ApiResponse.CreateFailure(errorResponse)),
            _ => BadRequest(ApiResponse.CreateFailure(errorResponse))
        };
    }
}
