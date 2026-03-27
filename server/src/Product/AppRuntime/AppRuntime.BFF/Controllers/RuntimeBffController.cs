using AppDefinition.Contracts.DTOs;
using AppRuntime.BFF.Services;
using AppRuntime.Contracts.DTOs;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TenantApplication.Application.DTOs;

namespace AppRuntime.BFF.Controllers;

/// <summary>
/// Runtime BFF: single backend surface for the runtime client.
/// Optimizes communication with the client; aggregates data from backend (in-process or Monolith/microservices).
/// In the future will handle auth tokens. Uses <see cref="IRuntimeApi"/> for resolve, snapshot, compatibility, execution.
/// </summary>
[ApiController]
[Route("api/runtime")]
public sealed class RuntimeBffController : ControllerBase
{
    private readonly IRuntimeApi _runtimeApi;

    public RuntimeBffController(IRuntimeApi runtimeApi)
    {
        _runtimeApi = runtimeApi;
    }

    /// <summary>Resolve application by URL. Returns ApplicationReleaseId and config for the given environment.</summary>
    [HttpGet("resolve")]
    [ProducesResponseType(typeof(ResolvedApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Resolve(
        [FromQuery] string tenantSlug,
        [FromQuery] string appSlug,
        [FromQuery] string? environment,
        CancellationToken cancellationToken)
    {
        var result = await _runtimeApi.ResolveAsync(
            tenantSlug ?? string.Empty,
            appSlug ?? string.Empty,
            environment ?? "production",
            cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.Error.Type == ErrorType.NotFound)
            return NotFound(ErrorBody(result.Error));
        return BadRequest(ErrorBody(result.Error));
    }

    /// <summary>Get initial view HTML by release ID. Returns text/html when available. 404 when not found (client falls back to JSON snapshot).</summary>
    [HttpGet("initial-view")]
    [Produces("text/html", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInitialViewHtml(
        [FromQuery] Guid applicationReleaseId,
        CancellationToken cancellationToken)
    {
        if (applicationReleaseId == Guid.Empty)
            return BadRequest(ErrorBody("Validation", "applicationReleaseId is required."));

        var result = await _runtimeApi.GetInitialViewHtmlAsync(applicationReleaseId, cancellationToken);
        if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            return Content(result.Value!, "text/html");
        if (result.IsFailure)
            return result.Error!.Type == ErrorType.NotFound ? NotFound(ErrorBody(result.Error)) : BadRequest(ErrorBody(result.Error));
        return NotFound(ErrorBody("NotFound", "Initial view HTML not found for this release."));
    }

    /// <summary>Get entity view HTML (list or form) by release ID, entity ID, and view type.</summary>
    [HttpGet("view")]
    [Produces("text/html", "application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEntityViewHtml(
        [FromQuery] Guid applicationReleaseId,
        [FromQuery] Guid entityId,
        [FromQuery] string viewType,
        CancellationToken cancellationToken)
    {
        if (applicationReleaseId == Guid.Empty)
            return BadRequest(ErrorBody("Validation", "applicationReleaseId is required."));
        if (entityId == Guid.Empty)
            return BadRequest(ErrorBody("Validation", "entityId is required."));
        if (string.IsNullOrWhiteSpace(viewType) || (viewType != "list" && viewType != "form"))
            return BadRequest(ErrorBody("Validation", "viewType must be 'list' or 'form'."));

        var result = await _runtimeApi.GetEntityViewHtmlAsync(applicationReleaseId, entityId, viewType, cancellationToken);
        if (result.IsSuccess && !string.IsNullOrEmpty(result.Value))
            return Content(result.Value!, "text/html");
        if (result.IsFailure)
            return result.Error!.Type == ErrorType.NotFound ? NotFound(ErrorBody(result.Error)) : BadRequest(ErrorBody(result.Error));
        return NotFound(ErrorBody("NotFound", "Entity view HTML not found."));
    }

    /// <summary>Get application snapshot by release ID (navigation, pages, data sources, entities).</summary>
    [HttpGet("snapshot")]
    [ProducesResponseType(typeof(ApplicationSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSnapshot(
        [FromQuery] Guid applicationReleaseId,
        CancellationToken cancellationToken)
    {
        if (applicationReleaseId == Guid.Empty)
            return BadRequest(ErrorBody("Validation", "applicationReleaseId is required."));

        var result = await _runtimeApi.GetSnapshotAsync(applicationReleaseId, cancellationToken);
        if (result.IsSuccess && result.Value != null)
            return Ok(result.Value);
        if (result.IsFailure)
            return result.Error!.Type == ErrorType.NotFound ? NotFound(ErrorBody(result.Error)) : BadRequest(ErrorBody(result.Error));
        return NotFound(ErrorBody("NotFound", "Release not found."));
    }

    /// <summary>Check compatibility of an application release with the runtime.</summary>
    [HttpGet("compatibility")]
    [ProducesResponseType(typeof(CompatibilityCheckResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckCompatibility(
        [FromQuery] Guid applicationReleaseId,
        [FromQuery] Guid? runtimeVersionId,
        CancellationToken cancellationToken)
    {
        if (applicationReleaseId == Guid.Empty)
            return BadRequest(ErrorBody("Validation", "applicationReleaseId is required."));

        var result = await _runtimeApi.CheckCompatibilityAsync(applicationReleaseId, runtimeVersionId, cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.Error!.Type == ErrorType.NotFound)
            return NotFound(ErrorBody(result.Error));
        return BadRequest(ErrorBody(result.Error));
    }

    /// <summary>Execute a datasource for the given release.</summary>
    [HttpPost("datasource/execute")]
    [ProducesResponseType(typeof(DatasourceExecuteResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteDatasource(
        [FromBody] DatasourceExecuteRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || request.ApplicationReleaseId == Guid.Empty)
            return BadRequest(ErrorBody("Validation", "applicationReleaseId is required."));
        if (string.IsNullOrWhiteSpace(request.DatasourceId))
            return BadRequest(ErrorBody("Validation", "datasourceId is required."));

        var result = await _runtimeApi.ExecuteDatasourceAsync(
            request.ApplicationReleaseId,
            request.DatasourceId,
            cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);
        if (result.Error!.Type == ErrorType.NotFound)
            return NotFound(ErrorBody(result.Error));
        return BadRequest(ErrorBody(result.Error));
    }

    private static object ErrorBody(Error error) =>
        new { code = error.Code, error = error.Message };

    private static object ErrorBody(string code, string message) =>
        new { code, error = message };
}
