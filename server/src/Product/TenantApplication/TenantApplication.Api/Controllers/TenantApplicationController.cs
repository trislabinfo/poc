using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TenantApplication.Api.Requests;
using TenantApplication.Application.Commands.ApproveMigration;
using TenantApplication.Application.Commands.ApproveTenantApplicationRelease;
using TenantApplication.Application.Commands.CreateCustomApplication;
using TenantApplication.Application.Commands.CreateEnvironment;
using TenantApplication.Application.Commands.CreateMigration;
using TenantApplication.Application.Commands.CreateTenantApplicationRelease;
using TenantApplication.Application.Commands.DeleteEnvironment;
using TenantApplication.Application.Commands.DeployToEnvironment;
using TenantApplication.Application.Commands.ForkApplication;
using TenantApplication.Application.Commands.InstallApplication;
using TenantApplication.Application.Commands.RunMigration;
using TenantApplication.Application.Commands.UpdateEnvironmentConfiguration;
using TenantApplication.Application.Commands.UpdateMigrationScript;
using TenantApplication.Application.Commands.UpdateTenantApplicationReleaseDdlScripts;
using TenantApplication.Application.DTOs;
using TenantApplication.Application.Queries.GetEnvironmentById;
using TenantApplication.Application.Queries.GetMigrationById;
using TenantApplication.Application.Queries.GetReleaseById;
using TenantApplication.Application.Queries.GetTenantApplicationById;
using TenantApplication.Application.Queries.GetTenantApplications;
using TenantApplication.Application.Queries.ListEnvironmentsByTenantApplication;
using TenantApplication.Application.Queries.ListMigrationsByEnvironment;
using TenantApplication.Application.Queries.ListReleasesByTenantApplication;

namespace TenantApplication.Api.Controllers;

[ApiController]
[Route("api/tenantapplication/tenants/{tenantId:guid}/applications")]
public sealed class TenantApplicationController : ControllerBase
{
    private readonly IRequestDispatcher _requestDispatcher;

    public TenantApplicationController(IRequestDispatcher requestDispatcher)
    {
        _requestDispatcher = requestDispatcher;
    }

    /// <summary>Get all applications for a tenant.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantApplicationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(Guid tenantId, CancellationToken cancellationToken)
    {
        var list = await _requestDispatcher.SendAsync(new GetTenantApplicationsQuery(tenantId), cancellationToken);
        return Ok(list);
    }

    /// <summary>Get tenant application by ID.</summary>
    [HttpGet("{tenantApplicationId:guid}")]
    [ProducesResponseType(typeof(TenantApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid tenantId, Guid tenantApplicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetTenantApplicationByIdQuery(tenantApplicationId), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok(result.Value);
    }

    /// <summary>Install a platform application for a tenant.</summary>
    [HttpPost("install")]
    [ProducesResponseType(typeof(TenantApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InstallApplication(
        Guid tenantId,
        [FromBody] InstallApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new InstallApplicationCommand(
                tenantId,
                request.ApplicationReleaseId,
                request.Name,
                request.Slug,
                request.ConfigurationJson),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.Conflict
                ? Conflict(new { code = result.Error.Code, message = result.Error.Message })
                : BadRequest(new { code = result.Error.Code, message = result.Error.Message });

        return CreatedAtAction(
            nameof(GetApplications),
            new { tenantId },
            result.Value);
    }

    /// <summary>Create a custom application for a tenant (AppBuilder feature).</summary>
    [HttpPost("custom")]
    [ProducesResponseType(typeof(TenantApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCustomApplication(
        Guid tenantId,
        [FromBody] CreateCustomApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new CreateCustomApplicationCommand(
                tenantId,
                request.Name,
                request.Slug,
                request.Description),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.Conflict
                ? Conflict(new { code = result.Error.Code, message = result.Error.Message })
                : BadRequest(new { code = result.Error.Code, message = result.Error.Message });

        return CreatedAtAction(
            nameof(GetApplications),
            new { tenantId },
            result.Value);
    }

    /// <summary>Create an environment (e.g. Development, Staging, Production).</summary>
    [HttpPost("{tenantApplicationId:guid}/environments")]
    [ProducesResponseType(typeof(TenantApplicationEnvironmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEnvironment(
        Guid tenantId,
        Guid tenantApplicationId,
        [FromBody] CreateEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var envType = request.EnvironmentType is >= 0 and <= 2
            ? (TenantApplication.Domain.Enums.EnvironmentType)request.EnvironmentType
            : TenantApplication.Domain.Enums.EnvironmentType.Development;
        var result = await _requestDispatcher.SendAsync(
            new CreateEnvironmentCommand(tenantId, tenantApplicationId, request.Name, envType),
            cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound)
                return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Not Found",
                    Status = 404,
                    Detail = result.Error.Message
                });
            return BadRequest(result.Error.Message);
        }
        return CreatedAtAction(nameof(GetEnvironment), new { tenantId, tenantApplicationId, environmentId = result.Value!.Id }, result.Value);
    }

    /// <summary>Get environment by ID.</summary>
    [HttpGet("{tenantApplicationId:guid}/environments/{environmentId:guid}")]
    [ProducesResponseType(typeof(TenantApplicationEnvironmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnvironment(Guid tenantId, Guid tenantApplicationId, Guid environmentId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetEnvironmentByIdQuery(environmentId), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok(result.Value);
    }

    /// <summary>List environments for a tenant application.</summary>
    [HttpGet("{tenantApplicationId:guid}/environments")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantApplicationEnvironmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListEnvironments(Guid tenantId, Guid tenantApplicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListEnvironmentsByTenantApplicationQuery(tenantApplicationId), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error.Message);
        return Ok(result.Value);
    }

    /// <summary>Update environment configuration.</summary>
    [HttpPut("{tenantApplicationId:guid}/environments/{environmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEnvironmentConfiguration(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        [FromBody] UpdateEnvironmentConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new UpdateEnvironmentConfigurationCommand(environmentId, request.ConfigurationJson), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok();
    }

    /// <summary>Delete environment.</summary>
    [HttpDelete("{tenantApplicationId:guid}/environments/{environmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEnvironment(Guid tenantId, Guid tenantApplicationId, Guid environmentId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new DeleteEnvironmentCommand(tenantApplicationId, environmentId), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return NoContent();
    }

    /// <summary>List releases for a tenant application.</summary>
    [HttpGet("{tenantApplicationId:guid}/releases")]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicationReleaseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReleases(Guid tenantId, Guid tenantApplicationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListReleasesByTenantApplicationQuery(tenantApplicationId), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error.Message);
        return Ok(result.Value);
    }

    /// <summary>Get release by ID.</summary>
    [HttpGet("{tenantApplicationId:guid}/releases/{releaseId:guid}")]
    [ProducesResponseType(typeof(ApplicationReleaseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelease(Guid tenantId, Guid tenantApplicationId, Guid releaseId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetReleaseByIdQuery(tenantApplicationId, releaseId), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok(result.Value);
    }

    /// <summary>Create a migration for an environment (from one release to another).</summary>
    [HttpPost("{tenantApplicationId:guid}/environments/{environmentId:guid}/migrations")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateMigration(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        [FromBody] CreateMigrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new CreateMigrationCommand(
                environmentId,
                request.FromReleaseId,
                request.ToReleaseId,
                request.MigrationScriptJson),
            cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return CreatedAtAction(nameof(GetMigration), new { tenantId, tenantApplicationId, environmentId, migrationId = result.Value }, new { id = result.Value });
    }

    /// <summary>List migrations for an environment.</summary>
    [HttpGet("{tenantApplicationId:guid}/environments/{environmentId:guid}/migrations")]
    [ProducesResponseType(typeof(IReadOnlyList<TenantApplicationMigrationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMigrations(Guid tenantId, Guid tenantApplicationId, Guid environmentId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new ListMigrationsByEnvironmentQuery(environmentId), cancellationToken);
        if (result.IsFailure) return BadRequest(result.Error.Message);
        return Ok(result.Value);
    }

    /// <summary>Get migration by ID.</summary>
    [HttpGet("{tenantApplicationId:guid}/environments/{environmentId:guid}/migrations/{migrationId:guid}")]
    [ProducesResponseType(typeof(TenantApplicationMigrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMigration(Guid tenantId, Guid tenantApplicationId, Guid environmentId, Guid migrationId, CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetMigrationByIdQuery(migrationId), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok(result.Value);
    }

    /// <summary>Fork a platform application into a custom tenant application (AppBuilder feature).</summary>
    [HttpPost("fork")]
    [ProducesResponseType(typeof(TenantApplicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ForkApplication(
        Guid tenantId,
        [FromBody] ForkApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new ForkApplicationCommand(
                tenantId,
                request.SourceApplicationReleaseId,
                request.Name,
                request.Slug),
            cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.Conflict ? Conflict() : BadRequest(result.Error.Message);
        return CreatedAtAction(nameof(GetById), new { tenantId, tenantApplicationId = result.Value!.Id }, result.Value);
    }

    /// <summary>Create a release from current definitions (custom/forked apps).</summary>
    [HttpPost("{tenantApplicationId:guid}/releases")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateRelease(
        Guid tenantId,
        Guid tenantApplicationId,
        [FromBody] CreateTenantApplicationReleaseRequest request,
        CancellationToken cancellationToken)
    {
        var releasedBy = Guid.Empty; // TODO: from auth
        var result = await _requestDispatcher.SendAsync(
            new CreateTenantApplicationReleaseCommand(
                tenantApplicationId,
                request.Major,
                request.Minor,
                request.Patch,
                request.ReleaseNotes ?? string.Empty,
                releasedBy),
            cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return CreatedAtAction(nameof(GetById), new { tenantId, tenantApplicationId }, new { id = result.Value });
    }

    /// <summary>Deploy a release to an environment. Returns migration ID if migration was created for review.</summary>
    [HttpPost("{tenantApplicationId:guid}/environments/{environmentId:guid}/deploy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeployToEnvironment(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        [FromBody] DeployToEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var deployedBy = Guid.Empty; // TODO: from auth
        var result = await _requestDispatcher.SendAsync(
            new DeployToEnvironmentCommand(
                tenantApplicationId,
                environmentId,
                request.ReleaseId,
                request.Version,
                deployedBy),
            cancellationToken);
        if (result.IsFailure)
        {
            if (result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound)
                return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Not Found",
                    Status = 404,
                    Detail = result.Error.Message
                });
            return BadRequest(result.Error.Message);
        }

        // If migration ID is returned, deployment requires migration approval
        if (result.Value.HasValue)
            return Ok(new { MigrationId = result.Value.Value, Message = "Migration created. Please review and approve before execution." });

        // Deployment completed immediately (first deployment)
        return Ok(new { Message = "Deployment completed successfully." });
    }

    /// <summary>Update migration script.</summary>
    [HttpPut("{tenantApplicationId:guid}/environments/{environmentId:guid}/migrations/{migrationId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMigrationScript(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        Guid migrationId,
        [FromBody] UpdateMigrationScriptRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new UpdateMigrationScriptCommand(
                tenantApplicationId,
                environmentId,
                migrationId,
                request.MigrationScriptJson),
            cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok();
    }

    /// <summary>Approve a migration (after review).</summary>
    [HttpPost("{tenantApplicationId:guid}/environments/{environmentId:guid}/migrations/{migrationId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveMigration(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        Guid migrationId,
        CancellationToken cancellationToken)
    {
        var approvedBy = Guid.Empty; // TODO: from auth context
        var result = await _requestDispatcher.SendAsync(
            new ApproveMigrationCommand(
                tenantApplicationId,
                environmentId,
                migrationId,
                approvedBy),
            cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok();
    }

    /// <summary>Run a migration (execute script against environment database).</summary>
    [HttpPost("{tenantApplicationId:guid}/environments/{environmentId:guid}/migrations/{migrationId:guid}/run")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunMigration(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        Guid migrationId,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new RunMigrationCommand(
                tenantApplicationId,
                environmentId,
                migrationId),
            cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);
        return Ok();
    }

    /// <summary>Get DDL scripts for a tenant application release.</summary>
    [HttpGet("{tenantApplicationId:guid}/releases/{releaseId:guid}/ddl-scripts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReleaseDdlScripts(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(new GetReleaseByIdQuery(tenantApplicationId, releaseId), cancellationToken);
        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);

        return Ok(new
        {
            DdlScriptsJson = result.Value?.DdlScriptsJson ?? "{}",
            DdlScriptsStatus = result.Value?.DdlScriptsStatus?.ToString() ?? "Pending",
            ApprovedAt = result.Value?.ApprovedAt,
            ApprovedBy = result.Value?.ApprovedBy
        });
    }

    /// <summary>Update DDL scripts for a tenant application release (before approval).</summary>
    [HttpPut("{tenantApplicationId:guid}/releases/{releaseId:guid}/ddl-scripts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReleaseDdlScripts(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid releaseId,
        [FromBody] UpdateReleaseDdlScriptsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _requestDispatcher.SendAsync(
            new UpdateTenantApplicationReleaseDdlScriptsCommand(
                tenantApplicationId,
                releaseId,
                request.DdlScriptsJson),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);

        return Ok();
    }

    /// <summary>Approve DDL scripts for a tenant application release.</summary>
    [HttpPost("{tenantApplicationId:guid}/releases/{releaseId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRelease(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid releaseId,
        CancellationToken cancellationToken)
    {
        var approvedBy = Guid.Empty; // TODO: from auth context
        var result = await _requestDispatcher.SendAsync(
            new ApproveTenantApplicationReleaseCommand(
                tenantApplicationId,
                releaseId,
                approvedBy),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.Type == BuildingBlocks.Kernel.Results.ErrorType.NotFound ? NotFound() : BadRequest(result.Error.Message);

        return Ok();
    }
}

public sealed record CreateEnvironmentRequest(string Name, int EnvironmentType); // 0=Development, 1=Staging, 2=Production
public sealed record UpdateEnvironmentConfigurationRequest(string? ConfigurationJson);
public sealed record CreateMigrationRequest(Guid? FromReleaseId, Guid ToReleaseId, string? MigrationScriptJson);
public sealed record UpdateMigrationScriptRequest(string MigrationScriptJson);
public sealed record UpdateReleaseDdlScriptsRequest(string DdlScriptsJson);
public sealed record ForkApplicationRequest(Guid SourceApplicationReleaseId, string Name, string Slug);
public sealed record CreateTenantApplicationReleaseRequest(int Major, int Minor, int Patch, string? ReleaseNotes);
public sealed record DeployToEnvironmentRequest(Guid ReleaseId, string Version);
