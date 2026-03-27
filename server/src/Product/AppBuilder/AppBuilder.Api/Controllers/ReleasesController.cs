using AppBuilder.Application.Commands.ActivateRelease;
using AppBuilder.Application.Commands.ApproveRelease;
using AppBuilder.Application.Commands.CreateApplicationRelease;
using AppBuilder.Application.Commands.DeactivateRelease;
using AppBuilder.Application.Commands.UpdateReleaseDdlScripts;
using AppBuilder.Application.Queries.GetApplicationRelease;
using AppBuilder.Application.Queries.GetReleaseSnapshot;
using AppBuilder.Application.Queries.ListReleasesByApplication;
using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AppBuilder.Api.Controllers;

/// <summary>REST API for application releases (create, list, activate/deactivate).</summary>
[ApiController]
[Route("api/appbuilder")]
public sealed class ReleasesController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public ReleasesController(IRequestDispatcher requestDispatcher) => _requestDispatcher = requestDispatcher;

    /// <summary>Create a release for an application (draft only).</summary>
    [HttpPost("applications/{applicationId:guid}/releases")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRelease(
        Guid applicationId,
        [FromBody] CreateReleaseBody body,
        CancellationToken cancellationToken)
    {
        var releasedBy = Guid.Empty; // TODO: from auth context
        var result = await _requestDispatcher.SendAsync(
            new CreateApplicationReleaseCommand(
                applicationId,
                body.Major,
                body.Minor,
                body.Patch,
                body.ReleaseNotes ?? string.Empty,
                releasedBy),
            cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == ErrorType.NotFound ? NotFound() : Problem(detail: result.Error.Message, statusCode: 400);
        return CreatedAtAction(nameof(GetRelease), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>Get release by ID.</summary>
    [HttpGet("releases/{id:guid}")]
    [ProducesResponseType(typeof(ApplicationReleaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelease(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetApplicationReleaseQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>Get application snapshot (navigation, pages, data sources, entities) for a release. Used by Runtime BFF.</summary>
    [HttpGet("releases/{id:guid}/snapshot")]
    [ProducesResponseType(typeof(ApplicationSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReleaseSnapshot(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetReleaseSnapshotQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>List releases for an application.</summary>
    [HttpGet("applications/{applicationId:guid}/releases")]
    [ProducesResponseType(typeof(List<ApplicationReleaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListByApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListReleasesByApplicationQuery(applicationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(detail: result.Error.Message, statusCode: 500);
    }

    /// <summary>Activate a release (deactivates other releases for the same application).</summary>
    [HttpPost("releases/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ActivateReleaseCommand(id), cancellationToken);
        return result.IsSuccess ? Ok() : NotFound();
    }

    /// <summary>Deactivate a release.</summary>
    [HttpPost("releases/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeactivateReleaseCommand(id), cancellationToken);
        return result.IsSuccess ? Ok() : NotFound();
    }

    /// <summary>Get DDL scripts for a release.</summary>
    [HttpGet("releases/{id:guid}/ddl-scripts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDdlScripts(Guid id, CancellationToken cancellationToken)
    {
        var releaseResult = await _requestDispatcher.SendAsync(new GetApplicationReleaseQuery(id), cancellationToken);
        if (releaseResult.IsFailure)
            return NotFound();

        return Ok(new
        {
            DdlScriptsJson = releaseResult.Value?.DdlScriptsJson ?? "{}",
            DdlScriptsStatus = releaseResult.Value?.DdlScriptsStatus?.ToString() ?? "Pending",
            ApprovedAt = releaseResult.Value?.ApprovedAt,
            ApprovedBy = releaseResult.Value?.ApprovedBy
        });
    }

    /// <summary>Update DDL scripts for a release (before approval).</summary>
    [HttpPut("applications/{applicationId:guid}/releases/{releaseId:guid}/ddl-scripts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDdlScripts(
        Guid applicationId,
        Guid releaseId,
        [FromBody] UpdateDdlScriptsBody body,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new UpdateReleaseDdlScriptsCommand(applicationId, releaseId, body.DdlScriptsJson),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Type == ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);

        return Ok();
    }

    /// <summary>Approve DDL scripts for a release.</summary>
    [HttpPost("applications/{applicationId:guid}/releases/{releaseId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRelease(
        Guid applicationId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var approvedBy = Guid.Empty; // TODO: from auth context
        var result = await _requestDispatcher.SendAsync(
            new ApproveReleaseCommand(applicationId, releaseId, approvedBy),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Type == ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);

        return Ok();
    }
}

public sealed record CreateReleaseBody(int Major, int Minor, int Patch, string? ReleaseNotes);
public sealed record UpdateDdlScriptsBody(string DdlScriptsJson);
