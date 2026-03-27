# TenantApplication Module - API Layer

**Status**: Ready for implementation (shared ApplicationDefinition)  
**Last Updated**: 2026-02-15  
**Module**: TenantApplication  
**Layer**: API  

---

## Implementation note

- API layer uses TenantApplication.Application (and shared definition types only via DTOs/responses as needed).  
- No direct project reference to ApplicationDefinition.Domain required unless the API exposes definition types directly.  
- **Review**: Confirm with other TenantApplication docs before implementation.

---

## Overview

REST API endpoints for managing tenant application installations and configurations. **When a tenant has the AppBuilder feature**, TenantApplication also exposes **definition CRUD** endpoints (entities, pages, navigation, data sources, releases) so that the same “AppBuilder” UX can edit tenant applications by calling this API. All definition endpoints are tenant-scoped (e.g. `api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/entities`) and require the tenant to have the AppBuilder feature enabled.

**Key Changes from Domain Update**:
- ✅ Updated routes to use Major/Minor/Patch
- ✅ Removed Version string from requests
- ✅ Added environment management endpoints

---

## Controllers

### TenantApplicationController

**File**: `TenantApplication.Api/Controllers/TenantApplicationController.cs`

```csharp
namespace Datarizen.TenantApplication.Api.Controllers;

[ApiController]
[Route("api/tenantapplication/tenants/{tenantId:guid}/applications")]
public sealed class TenantApplicationController : ControllerBase
{
    private readonly ISender _sender;

    public TenantApplicationController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Install an application for a tenant
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InstallApplication(
        Guid tenantId,
        [FromBody] InstallApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new InstallApplicationCommand(
            tenantId,
            request.ApplicationId,
            request.Major,
            request.Minor,
            request.Patch,
            request.Name,
            request.Slug,
            request.Configuration);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(
                nameof(GetTenantApplication),
                new { tenantId, tenantApplicationId = result.Value.Id },
                ApiResponse<TenantApplicationDto>.Success(result.Value))
            : result.Error.Type == ErrorType.NotFound
                ? NotFound(ApiResponse.Failure(result.Error))
                : result.Error.Type == ErrorType.Conflict
                    ? Conflict(ApiResponse.Failure(result.Error))
                    : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get all applications for a tenant
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<TenantApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantApplications(
        Guid tenantId,
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetTenantApplicationsQuery(tenantId, activeOnly);
        var result = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<List<TenantApplicationDto>>.Success(result.Value));
    }

    /// <summary>
    /// Get tenant application by ID
    /// </summary>
    [HttpGet("{tenantApplicationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantApplication(
        Guid tenantId,
        Guid tenantApplicationId,
        CancellationToken cancellationToken)
    {
        var query = new GetTenantApplicationQuery(tenantApplicationId);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<TenantApplicationDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Update tenant application configuration
    /// </summary>
    [HttpPut("{tenantApplicationId:guid}/configuration")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateConfiguration(
        Guid tenantId,
        Guid tenantApplicationId,
        [FromBody] UpdateConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateConfigurationCommand(
            tenantApplicationId,
            request.Configuration);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<TenantApplicationDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Upgrade application to a new version
    /// </summary>
    [HttpPost("{tenantApplicationId:guid}/upgrade")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpgradeApplication(
        Guid tenantId,
        Guid tenantApplicationId,
        [FromBody] UpgradeApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpgradeApplicationCommand(
            tenantApplicationId,
            request.Major,
            request.Minor,
            request.Patch);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<TenantApplicationDto>.Success(result.Value))
            : result.Error.Type == ErrorType.NotFound
                ? NotFound(ApiResponse.Failure(result.Error))
                : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Activate application
    /// </summary>
    [HttpPost("{tenantApplicationId:guid}/activate")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateApplication(
        Guid tenantId,
        Guid tenantApplicationId,
        CancellationToken cancellationToken)
    {
        var command = new ActivateApplicationCommand(tenantApplicationId);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<TenantApplicationDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Deactivate application
    /// </summary>
    [HttpPost("{tenantApplicationId:guid}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateApplication(
        Guid tenantId,
        Guid tenantApplicationId,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateApplicationCommand(tenantApplicationId);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<TenantApplicationDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Uninstall application
    /// </summary>
    [HttpDelete("{tenantApplicationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UninstallApplication(
        Guid tenantId,
        Guid tenantApplicationId,
        CancellationToken cancellationToken)
    {
        var command = new UninstallApplicationCommand(tenantApplicationId);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Create environment for tenant application
    /// </summary>
    [HttpPost("{tenantApplicationId:guid}/environments")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationEnvironmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEnvironment(
        Guid tenantId,
        Guid tenantApplicationId,
        [FromBody] CreateEnvironmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEnvironmentCommand(
            tenantApplicationId,
            request.Name,
            request.EnvironmentType,
            request.Configuration);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(
                nameof(GetEnvironment),
                new { tenantId, tenantApplicationId, environmentId = result.Value.Id },
                ApiResponse<TenantApplicationEnvironmentDto>.Success(result.Value))
            : result.Error.Type == ErrorType.NotFound
                ? NotFound(ApiResponse.Failure(result.Error))
                : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get environment by ID
    /// </summary>
    [HttpGet("{tenantApplicationId:guid}/environments/{environmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationEnvironmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEnvironment(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        CancellationToken cancellationToken)
    {
        var query = new GetEnvironmentQuery(tenantApplicationId, environmentId);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<TenantApplicationEnvironmentDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Update environment configuration
    /// </summary>
    [HttpPut("{tenantApplicationId:guid}/environments/{environmentId:guid}/configuration")]
    [ProducesResponseType(typeof(ApiResponse<TenantApplicationEnvironmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEnvironmentConfiguration(
        Guid tenantId,
        Guid tenantApplicationId,
        Guid environmentId,
        [FromBody] UpdateEnvironmentConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEnvironmentConfigurationCommand(
            tenantApplicationId,
            environmentId,
            request.Configuration);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<TenantApplicationEnvironmentDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }
}
```

---

## Request Models

### InstallApplicationRequest

```csharp
namespace Datarizen.TenantApplication.Api.Models;

public sealed record InstallApplicationRequest
{
    public Guid ApplicationId { get; init; }
    public int Major { get; init; }
    public int Minor { get; init; }
    public int Patch { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public Dictionary<string, object>? Configuration { get; init; }
}
```

### UpdateConfigurationRequest

```csharp
namespace Datarizen.TenantApplication.Api.Models;

public sealed record UpdateConfigurationRequest
{
    public Dictionary<string, object> Configuration { get; init; } = new();
}
```

### UpgradeApplicationRequest

```csharp
namespace Datarizen.TenantApplication.Api.Models;

public sealed record UpgradeApplicationRequest
{
    public int Major { get; init; }
    public int Minor { get; init; }
    public int Patch { get; init; }
}
```

### CreateEnvironmentRequest

```csharp
namespace Datarizen.TenantApplication.Api.Models;

public sealed record CreateEnvironmentRequest
{
    public string Name { get; init; } = string.Empty;
    public string EnvironmentType { get; init; } = string.Empty;
    public Dictionary<string, object>? Configuration { get; init; }
}
```

### UpdateEnvironmentConfigurationRequest

```csharp
namespace Datarizen.TenantApplication.Api.Models;

public sealed record UpdateEnvironmentConfigurationRequest
{
    public Dictionary<string, object> Configuration { get; init; } = new();
}
```

---

## Definition CRUD endpoints (AppBuilder feature)

When a tenant has the AppBuilder feature, the AppBuilder UX calls **TenantApplication** API to edit that tenant’s application. The following resource trees mirror AppBuilder’s API but are **tenant-scoped**; every request must validate tenant context and “tenant has AppBuilder feature.”

**Base path**: `api/tenantapplication/tenants/{tenantId:guid}/applications/{tenantApplicationId:guid}`

**Endpoints to implement** (same HTTP verbs and request/response shape as AppBuilder where applicable):

| Resource | GET (list) | GET (by id) | POST (create) | PUT (update) | DELETE |
|----------|------------|-------------|----------------|--------------|--------|
| Entities | `.../entities` | `.../entities/{id}` | `.../entities` | `.../entities/{id}` | `.../entities/{id}` |
| Properties (under entity) | `.../entities/{entityId}/properties` | `.../entities/{entityId}/properties/{id}` | `.../entities/{entityId}/properties` | `.../entities/{entityId}/properties/{id}` | `.../entities/{entityId}/properties/{id}` |
| Relations | `.../relations` (filter by tenant app) | `.../relations/{id}` | `.../relations` | `.../relations/{id}` | `.../relations/{id}` |
| Navigation | `.../navigation` | `.../navigation/{id}` | `.../navigation` | `.../navigation/{id}` | `.../navigation/{id}` |
| Pages | `.../pages` | `.../pages/{id}` | `.../pages` | `.../pages/{id}` | `.../pages/{id}` |
| Data sources | `.../datasources` | `.../datasources/{id}` | `.../datasources` | `.../datasources/{id}` | `.../datasources/{id}` |
| Releases | `.../releases` | `.../releases/{id}` or by version | `.../releases` (create release from current definitions) | — | — |

**Additional**:
- **POST** `api/tenantapplication/tenants/{tenantId}/applications` with body `{ name, slug, description }` → Create custom application (TenantApplication, IsCustom = true, Draft).
- **POST** `api/tenantapplication/tenants/{tenantId}/applications/{tenantApplicationId}/fork` with body `{ sourceApplicationReleaseId, name, slug }` → Fork a platform release into this tenant app.

Request/response models should align with AppBuilder’s DTOs (e.g. entity name, route, configuration JSON) so the same UI components can call either AppBuilder or TenantApplication API.

---

## Service Registration

**File**: `TenantApplication.Api/DependencyInjection.cs`

```csharp
namespace Datarizen.TenantApplication.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddTenantApplicationApi(
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
- ✅ All CRUD operations for tenant applications (install, config, upgrade, activate, deactivate, uninstall)
- ✅ Environment management endpoints
- ✅ **Definition CRUD endpoints** for tenant applications when AppBuilder feature is enabled (entities, pages, navigation, data sources, relations, releases; tenant-scoped)
- ✅ Proper HTTP status codes (200, 201, 204, 400, 404, 409)
- ✅ XML documentation on all endpoints
- ✅ Request models defined
- ✅ Service registration complete

