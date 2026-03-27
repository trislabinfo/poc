# AppRuntime Module - API Layer

**Status**: ✅ Updated to align with new domain model  
**Last Updated**: 2026-02-11  
**Module**: AppRuntime  
**Layer**: API  

---

## Overview

REST API endpoints for executing applications.

**Key Changes from Domain Update**:
- ✅ Updated routes to use Major/Minor/Patch
- ✅ Removed Version string from requests
- ✅ Added engine version parameters

---

## Controllers

### ApplicationRuntimeController

**File**: `AppRuntime.Api/Controllers/ApplicationRuntimeController.cs`

```csharp
namespace Datarizen.AppRuntime.Api.Controllers;

[ApiController]
[Route("api/appruntime/applications/{applicationId:guid}")]
public sealed class ApplicationRuntimeController : ControllerBase
{
    private readonly ISender _sender;

    public ApplicationRuntimeController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Get active release for an application
    /// </summary>
    [HttpGet("active-release")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationReleaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActiveRelease(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var query = new GetActiveReleaseQuery(applicationId);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<ApplicationReleaseDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get specific release by version
    /// </summary>
    [HttpGet("releases/{major:int}.{minor:int}.{patch:int}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationReleaseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRelease(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        CancellationToken cancellationToken)
    {
        var query = new GetApplicationReleaseQuery(applicationId, major, minor, patch);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<ApplicationReleaseDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Execute navigation for a specific release
    /// </summary>
    [HttpPost("releases/{major:int}.{minor:int}.{patch:int}/navigation/execute")]
    [ProducesResponseType(typeof(ApiResponse<NavigationExecutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteNavigation(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        [FromBody] ExecuteNavigationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ExecuteNavigationCommand(
            applicationId,
            major,
            minor,
            patch,
            request.EngineVersion,
            request.Context);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<NavigationExecutionResult>.Success(result.Value))
            : result.Error.Type == ErrorType.NotFound
                ? NotFound(ApiResponse.Failure(result.Error))
                : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Execute page for a specific release
    /// </summary>
    [HttpPost("releases/{major:int}.{minor:int}.{patch:int}/pages/execute")]
    [ProducesResponseType(typeof(ApiResponse<PageExecutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecutePage(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        [FromBody] ExecutePageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ExecutePageCommand(
            applicationId,
            major,
            minor,
            patch,
            request.Route,
            request.EngineVersion,
            request.Context);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<PageExecutionResult>.Success(result.Value))
            : result.Error.Type == ErrorType.NotFound
                ? NotFound(ApiResponse.Failure(result.Error))
                : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Execute data source for a specific release
    /// </summary>
    [HttpPost("releases/{major:int}.{minor:int}.{patch:int}/datasources/execute")]
    [ProducesResponseType(typeof(ApiResponse<DataSourceExecutionResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExecuteDataSource(
        Guid applicationId,
        int major,
        int minor,
        int patch,
        [FromBody] ExecuteDataSourceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ExecuteDataSourceCommand(
            applicationId,
            major,
            minor,
            patch,
            request.DataSourceName,
            request.EngineVersion,
            request.Parameters);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<DataSourceExecutionResult>.Success(result.Value))
            : result.Error.Type == ErrorType.NotFound
                ? NotFound(ApiResponse.Failure(result.Error))
                : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get available engine versions
    /// </summary>
    [HttpGet("engine-versions")]
    [ProducesResponseType(typeof(ApiResponse<AvailableEngineVersionsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableEngineVersions(
        CancellationToken cancellationToken)
    {
        var query = new GetAvailableEngineVersionsQuery();
        var result = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<AvailableEngineVersionsDto>.Success(result.Value));
    }
}
```

---

## Request Models

### ExecuteNavigationRequest

```csharp
namespace Datarizen.AppRuntime.Api.Models;

public sealed record ExecuteNavigationRequest
{
    public string EngineVersion { get; init; } = "v1";
    public Dictionary<string, object> Context { get; init; } = new();
}
```

### ExecutePageRequest

```csharp
namespace Datarizen.AppRuntime.Api.Models;

public sealed record ExecutePageRequest
{
    public string Route { get; init; } = string.Empty;
    public string EngineVersion { get; init; } = "v1";
    public Dictionary<string, object> Context { get; init; } = new();
}
```

### ExecuteDataSourceRequest

```csharp
namespace Datarizen.AppRuntime.Api.Models;

public sealed record ExecuteDataSourceRequest
{
    public string DataSourceName { get; init; } = string.Empty;
    public string EngineVersion { get; init; } = "v1";
    public Dictionary<string, object> Parameters { get; init; } = new();
}
```

---

## Service Registration

**File**: `AppRuntime.Api/DependencyInjection.cs`

```csharp
namespace Datarizen.AppRuntime.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddAppRuntimeApi(
        this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(DependencyInjection).Assembly);

        return services;
    }
}
```

---

## Success Criteria

- ✅ Routes use Major/Minor/Patch format
- ✅ Engine version parameter added to execution endpoints
- ✅ Proper HTTP status codes (200, 400, 404)
- ✅ XML documentation on all endpoints
- ✅ Request models defined
- ✅ Service registration complete

