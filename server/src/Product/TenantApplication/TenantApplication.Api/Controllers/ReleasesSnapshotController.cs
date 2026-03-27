using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TenantApplication.Application.DTOs;
using TenantApplication.Application.Queries.GetReleaseSnapshot;
using TenantApplication.Application.Services;

namespace TenantApplication.Api.Controllers;

/// <summary>Runtime/BFF-facing endpoints: resolve by URL, snapshot by release ID.</summary>
[ApiController]
[Route("api/tenantapplication")]
public sealed class ReleasesSnapshotController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;
    private readonly IApplicationResolverService _resolver;

    public ReleasesSnapshotController(IRequestDispatcher requestDispatcher, IApplicationResolverService resolver)
    {
        _requestDispatcher = requestDispatcher;
        _resolver = resolver;
    }

    /// <summary>Resolve application by URL (tenant slug, app slug, environment). Used by Runtime BFF.</summary>
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
        var result = await _resolver.ResolveByUrlAsync(
            tenantSlug ?? string.Empty,
            appSlug ?? string.Empty,
            environment ?? "production",
            cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);
        return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound
            ? NotFound()
            : BadRequest(new { code = result.Error.Code, message = result.Error.Message });
    }

    /// <summary>Get application snapshot by release ID. Used by Runtime BFF for tenant-owned releases.</summary>
    [HttpGet("releases/{releaseId:guid}/snapshot")]
    [ProducesResponseType(typeof(ApplicationSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSnapshot(Guid releaseId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetReleaseSnapshotQuery(releaseId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }
}
