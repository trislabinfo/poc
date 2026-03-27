using AppRuntime.Contracts.DTOs;
using AppRuntime.Contracts.Services;
using BuildingBlocks.Kernel.Results;
using BuildingBlocks.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppRuntime.Api.Controllers;

[ApiController]
[Route("api/appruntime")]
public sealed class CompatibilityController : ControllerBase
{
    private readonly ICompatibilityCheckService _compatibilityCheckService;

    public CompatibilityController(ICompatibilityCheckService compatibilityCheckService)
    {
        _compatibilityCheckService = compatibilityCheckService;
    }

    /// <summary>
    /// Check if the current (or specified) runtime version can execute the given application release.
    /// </summary>
    /// <param name="applicationReleaseId">Application release ID.</param>
    /// <param name="runtimeVersionId">Optional runtime version ID. Omit to use current runtime.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("compatibility")]
    [ProducesResponseType(typeof(ApiResponse<CompatibilityCheckResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckCompatibility(
        [FromQuery] Guid applicationReleaseId,
        [FromQuery] Guid? runtimeVersionId,
        CancellationToken cancellationToken = default)
    {
        if (applicationReleaseId == Guid.Empty)
            return BadRequest(ApiResponse.CreateFailure(new ErrorResponse("Validation", "applicationReleaseId is required.")));

        Result<CompatibilityCheckResultDto> result = runtimeVersionId.HasValue
            ? await _compatibilityCheckService.CheckCompatibilityAsync(applicationReleaseId, runtimeVersionId.Value, cancellationToken)
            : await _compatibilityCheckService.CheckCompatibilityAsync(applicationReleaseId, cancellationToken);

        if (result.IsSuccess)
            return Ok(ApiResponse<CompatibilityCheckResultDto>.CreateSuccess(result.Value));

        var errorResponse = new ErrorResponse(
            result.Error.Code,
            result.Error.Message,
            HttpContext.Items["CorrelationId"]?.ToString(),
            DateTime.UtcNow);
        var envelope = ApiResponse.CreateFailure(errorResponse);

        return result.Error.Type switch
        {
            ErrorType.Validation => BadRequest(envelope),
            ErrorType.NotFound => NotFound(envelope),
            ErrorType.Conflict => Conflict(envelope),
            ErrorType.Unauthorized => Unauthorized(envelope),
            ErrorType.Forbidden => StatusCode(403, envelope),
            _ => StatusCode(500, envelope)
        };
    }
}
