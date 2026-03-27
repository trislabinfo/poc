# Feature Module - API Layer (MVP)

## Overview

The Feature module provides **platform-level feature management**. Features can have multiple flags for granular control.

**Important**: This module does NOT handle tenant-specific feature assignments. Tenant feature management is handled by the Tenant module via `TenantFeature` entity.

---

## REST Endpoints

### Platform Features (Admin Only)

#### Create Feature
```http
POST /api/feature/features
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Notification",
  "description": "Notification system features",
  "isEnabled": true
}

Response: 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### Update Feature
```http
PUT /api/feature/features/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Notification System",
  "description": "Updated description"
}

Response: 204 No Content
```

#### Enable/Disable Feature
```http
POST /api/feature/features/{id}/enable
POST /api/feature/features/{id}/disable
Authorization: Bearer {token}

Response: 204 No Content
```

#### Get All Features
```http
GET /api/feature/features
Authorization: Bearer {token}

Response: 200 OK
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Notification",
    "description": "Notification system features",
    "isEnabled": true,
    "createdAt": "2026-02-14T10:00:00Z",
    "updatedAt": "2026-02-14T11:00:00Z"
  }
]
```

#### Get Feature by ID
```http
GET /api/feature/features/{id}
Authorization: Bearer {token}

Response: 200 OK
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Notification",
  "description": "Notification system features",
  "isEnabled": true,
  "flags": [
    {
      "id": "flag-1",
      "name": "SmsNotification",
      "description": "SMS notification support",
      "isEnabled": true
    },
    {
      "id": "flag-2",
      "name": "EmailNotification",
      "description": "Email notification support",
      "isEnabled": true
    }
  ],
  "createdAt": "2026-02-14T10:00:00Z",
  "updatedAt": null
}
```

---

### Feature Flags

#### Create Feature Flag
```http
POST /api/feature/flags
Authorization: Bearer {token}
Content-Type: application/json

{
  "featureId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "PushNotification",
  "description": "Push notification support",
  "isEnabled": false
}

Response: 201 Created
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### Update Feature Flag
```http
PUT /api/feature/flags/{id}
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Push Notifications",
  "description": "Updated description"
}

Response: 204 No Content
```

#### Enable/Disable Feature Flag
```http
POST /api/feature/flags/{id}/enable
POST /api/feature/flags/{id}/disable
Authorization: Bearer {token}

Response: 204 No Content
```

#### Get Feature Flags by Feature
```http
GET /api/feature/features/{featureId}/flags
Authorization: Bearer {token}

Response: 200 OK
[
  {
    "id": "flag-1",
    "featureId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "SmsNotification",
    "description": "SMS notification support",
    "isEnabled": true,
    "createdAt": "2026-02-14T10:00:00Z",
    "updatedAt": null
  }
]
```

#### Get Feature Flag by ID
```http
GET /api/feature/flags/{id}
Authorization: Bearer {token}

Response: 200 OK
{
  "id": "flag-1",
  "featureId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "SmsNotification",
  "description": "SMS notification support",
  "isEnabled": true,
  "createdAt": "2026-02-14T10:00:00Z",
  "updatedAt": null
}
```

#### Delete Feature Flag
```http
DELETE /api/feature/flags/{id}
Authorization: Bearer {token}

Response: 204 No Content
```

---

## Controller Implementation

### FeatureController

**File**: `Feature.Api/Controllers/FeatureController.cs`

```csharp
namespace Datarizen.Feature.Api.Controllers;

[ApiController]
[Route("api/feature/features")]
[Authorize(Roles = "Admin")]
public sealed class FeatureController : ControllerBase
{
    private readonly ISender _sender;

    public FeatureController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Create a new feature
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFeature(
        [FromBody] CreateFeatureRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFeatureCommand(
            request.Name,
            request.Description,
            request.IsEnabled);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetFeatureById), new { id = result.Value }, ApiResponse<Guid>.Success(result.Value))
            : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get all features
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<FeatureDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllFeatures(CancellationToken cancellationToken)
    {
        var query = new GetAllFeaturesQuery();
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<List<FeatureDto>>.Success(result.Value))
            : StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get feature by ID (includes flags)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeatureDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeatureById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetFeatureByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<FeatureDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Update feature
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFeature(
        Guid id,
        [FromBody] UpdateFeatureRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFeatureCommand(id, request.Name, request.Description);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Enable feature globally
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableFeature(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new EnableFeatureCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Disable feature globally
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableFeature(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DisableFeatureCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Delete feature
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeature(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFeatureCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get all flags for a feature
    /// </summary>
    [HttpGet("{featureId:guid}/flags")]
    [ProducesResponseType(typeof(ApiResponse<List<FeatureFlagDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeatureFlags(
        Guid featureId,
        CancellationToken cancellationToken)
    {
        var query = new GetFeatureFlagsByFeatureQuery(featureId);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<List<FeatureFlagDto>>.Success(result.Value))
            : StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Failure(result.Error));
    }
}
```

### FeatureFlagController

**File**: `Feature.Api/Controllers/FeatureFlagController.cs`

```csharp
namespace Datarizen.Feature.Api.Controllers;

[ApiController]
[Route("api/feature/flags")]
[Authorize(Roles = "Admin")]
public sealed class FeatureFlagController : ControllerBase
{
    private readonly ISender _sender;

    public FeatureFlagController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Create a new feature flag
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFeatureFlag(
        [FromBody] CreateFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateFeatureFlagCommand(
            request.FeatureId,
            request.Name,
            request.Description,
            request.IsEnabled);

        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetFeatureFlagById), new { id = result.Value }, ApiResponse<Guid>.Success(result.Value))
            : BadRequest(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Get feature flag by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FeatureFlagDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFeatureFlagById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetFeatureFlagByIdQuery(id);
        var result = await _sender.Send(query, cancellationToken);

        return result.IsSuccess
            ? Ok(ApiResponse<FeatureFlagDto>.Success(result.Value))
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Update feature flag
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFeatureFlag(
        Guid id,
        [FromBody] UpdateFeatureFlagRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateFeatureFlagCommand(id, request.Name, request.Description);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Enable feature flag
    /// </summary>
    [HttpPost("{id:guid}/enable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableFeatureFlag(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new EnableFeatureFlagCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Disable feature flag
    /// </summary>
    [HttpPost("{id:guid}/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisableFeatureFlag(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DisableFeatureFlagCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }

    /// <summary>
    /// Delete feature flag
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFeatureFlag(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new DeleteFeatureFlagCommand(id);
        var result = await _sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : NotFound(ApiResponse.Failure(result.Error));
    }
}
```

---

## Request/Response Models

### CreateFeatureRequest

```csharp
namespace Datarizen.Feature.Api.Models;

public sealed record CreateFeatureRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}
```

### UpdateFeatureRequest

```csharp
namespace Datarizen.Feature.Api.Models;

public sealed record UpdateFeatureRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
```

### CreateFeatureFlagRequest

```csharp
namespace Datarizen.Feature.Api.Models;

public sealed record CreateFeatureFlagRequest
{
    public Guid FeatureId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}
```

### UpdateFeatureFlagConfigurationRequest

```csharp
namespace Datarizen.Feature.Api.Models;

public sealed record UpdateFeatureFlagConfigurationRequest
{
    public string? Configuration { get; init; }
}
```

---

## API Routes Summary

### Feature Management

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/feature/features` | Create feature |
| GET | `/api/feature/features` | Get all features |
| GET | `/api/feature/features/{id}` | Get feature by ID |
| GET | `/api/feature/features/by-code/{code}` | Get feature by code |
| GET | `/api/feature/features/by-category/{category}` | Get features by category |
| PUT | `/api/feature/features/{id}` | Update feature |
| POST | `/api/feature/features/{id}/enable` | Enable feature globally |
| POST | `/api/feature/features/{id}/disable` | Disable feature globally |
| DELETE | `/api/feature/features/{id}` | Delete feature |

### Feature Flag Management

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/feature/flags` | Create feature flag |
| GET | `/api/feature/flags/{id}` | Get feature flag by ID |
| GET | `/api/feature/flags/tenant/{tenantId}` | Get flags for tenant |
| POST | `/api/feature/flags/{id}/toggle` | Toggle feature flag |
| PUT | `/api/feature/flags/{id}/configuration` | Update flag configuration |
| DELETE | `/api/feature/flags/{id}` | Delete feature flag |

### Feature Evaluation

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/feature/evaluation/{code}?tenantId={tenantId}&userId={userId}` | Check if feature is enabled |

---

## Success Criteria

- ✅ 3 controllers (Feature, FeatureFlag, FeatureEvaluation)
- ✅ All CRUD endpoints for features
- ✅ All CRUD endpoints for feature flags
- ✅ Hierarchical evaluation endpoint
- ✅ Request/response models defined
- ✅ Proper HTTP status codes
- ✅ API documentation comments



