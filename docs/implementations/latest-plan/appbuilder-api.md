# AppBuilder Module - API Layer

**Status**: ✅ Updated to align with new domain model  
**Last Updated**: 2026-02-11  
**Module**: AppBuilder  
**Layer**: API  

---

## Overview

REST API endpoints for the AppBuilder module. **Scope: platform applications only.** These endpoints operate on the `appbuilder` schema. For editing **tenant** applications (when the tenant has the AppBuilder feature), use the **TenantApplication** API (tenant-scoped definition CRUD); the same AppBuilder UX can call either this API or TenantApplication API depending on context.

**Key Changes from Domain Update**:
- ❌ Removed ApplicationSchema endpoints
- ✅ Added EntityDefinition endpoints
- ✅ Added PropertyDefinition endpoints
- ✅ Added RelationDefinition endpoints
- ✅ Renamed *Component endpoints to *Definition
- ✅ Updated ApplicationRelease endpoints (Major/Minor/Patch)

---

## 1. ApplicationDefinition Endpoints

### Create Application
```http
POST /api/appbuilder/applications
Content-Type: application/json

{
  "name": "CRM System",
  "description": "Customer relationship management",
  "slug": "crm",
  "isPublic": false
}

Response: 201 Created
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### Update Application
```http
PUT /api/appbuilder/applications/{id}
Content-Type: application/json

{
  "name": "CRM System Pro",
  "description": "Advanced CRM"
}

Response: 200 OK
```

### Get Application
```http
GET /api/appbuilder/applications/{id}

Response: 200 OK
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "CRM System",
  "description": "Customer relationship management",
  "slug": "crm",
  "status": "Draft",
  "currentVersion": null,
  "isPublic": false,
  "createdAt": "2026-02-11T10:00:00Z",
  "updatedAt": null
}
```

### List Applications
```http
GET /api/appbuilder/applications?status=Draft

Response: 200 OK
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "CRM System",
    "slug": "crm",
    "status": "Draft",
    "currentVersion": null,
    "isPublic": false,
    "createdAt": "2026-02-11T10:00:00Z"
  }
]
```

### Delete Application
```http
DELETE /api/appbuilder/applications/{id}

Response: 204 No Content
```

---

## 2. EntityDefinition Endpoints

### Create Entity
```http
POST /api/appbuilder/entities
Content-Type: application/json

{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Customer",
  "displayName": "Customer",
  "description": "Customer entity",
  "attributes": {
    "icon": "user",
    "color": "blue"
  },
  "primaryKey": "id"
}

Response: 201 Created
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66afa7"
}
```

### Update Entity
```http
PUT /api/appbuilder/entities/{id}
Content-Type: application/json

{
  "displayName": "Customer Record",
  "description": "Updated description",
  "attributes": {
    "icon": "users",
    "color": "green"
  }
}

Response: 200 OK
```

### Get Entity
```http
GET /api/appbuilder/entities/{id}

Response: 200 OK
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Customer",
  "displayName": "Customer",
  "description": "Customer entity",
  "attributes": {
    "icon": "user",
    "color": "blue"
  },
  "primaryKey": "id",
  "createdAt": "2026-02-11T10:00:00Z"
}
```

### Get Entity with Properties
```http
GET /api/appbuilder/entities/{id}/details

Response: 200 OK
{
  "id": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "name": "Customer",
  "displayName": "Customer",
  "properties": [
    {
      "id": "8fa85f64-5717-4562-b3fc-2c963f66afa8",
      "name": "firstName",
      "displayName": "First Name",
      "dataType": "String",
      "isRequired": true,
      "order": 1
    }
  ],
  "relations": [
    {
      "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
      "targetEntityId": "...",
      "name": "orders",
      "relationType": "OneToMany",
      "cascadeDelete": false
    }
  ]
}
```

### List Entities by Application
```http
GET /api/appbuilder/applications/{applicationId}/entities

Response: 200 OK
[
  {
    "id": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
    "name": "Customer",
    "displayName": "Customer",
    "createdAt": "2026-02-11T10:00:00Z"
  }
]
```

### Delete Entity
```http
DELETE /api/appbuilder/entities/{id}

Response: 204 No Content
```

---

## 3. PropertyDefinition Endpoints

### Create Property
```http
POST /api/appbuilder/properties
Content-Type: application/json

{
  "entityId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "name": "firstName",
  "displayName": "First Name",
  "dataType": "String",
  "isRequired": true,
  "defaultValue": null,
  "validationRules": {
    "minLength": 2,
    "maxLength": 100
  },
  "order": 1
}

Response: 201 Created
{
  "id": "8fa85f64-5717-4562-b3fc-2c963f66afa8"
}
```

### Update Property
```http
PUT /api/appbuilder/properties/{id}
Content-Type: application/json

{
  "displayName": "First Name (Required)",
  "isRequired": true,
  "defaultValue": "",
  "validationRules": {
    "minLength": 1,
    "maxLength": 150
  },
  "order": 1
}

Response: 200 OK
```

### Get Property
```http
GET /api/appbuilder/properties/{id}

Response: 200 OK
{
  "id": "8fa85f64-5717-4562-b3fc-2c963f66afa8",
  "entityId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "name": "firstName",
  "displayName": "First Name",
  "dataType": "String",
  "isRequired": true,
  "defaultValue": null,
  "validationRules": {
    "minLength": 2,
    "maxLength": 100
  },
  "order": 1,
  "createdAt": "2026-02-11T10:00:00Z"
}
```

### List Properties by Entity
```http
GET /api/appbuilder/entities/{entityId}/properties

Response: 200 OK
[
  {
    "id": "8fa85f64-5717-4562-b3fc-2c963f66afa8",
    "name": "firstName",
    "displayName": "First Name",
    "dataType": "String",
    "isRequired": true,
    "order": 1
  }
]
```

### Reorder Properties
```http
PUT /api/appbuilder/entities/{entityId}/properties/reorder
Content-Type: application/json

{
  "propertyIds": [
    "8fa85f64-5717-4562-b3fc-2c963f66afa8",
    "9fa85f64-5717-4562-b3fc-2c963f66afa9",
    "afa85f64-5717-4562-b3fc-2c963f66afaa"
  ]
}

Response: 200 OK
```

### Delete Property
```http
DELETE /api/appbuilder/properties/{id}

Response: 204 No Content
```

---

## 4. RelationDefinition Endpoints

### Create Relation
```http
POST /api/appbuilder/relations
Content-Type: application/json

{
  "sourceEntityId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "targetEntityId": "bfa85f64-5717-4562-b3fc-2c963f66afab",
  "name": "orders",
  "relationType": "OneToMany",
  "cascadeDelete": false
}

Response: 201 Created
{
  "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9"
}
```

### Update Relation
```http
PUT /api/appbuilder/relations/{id}
Content-Type: application/json

{
  "relationType": "OneToMany",
  "cascadeDelete": true
}

Response: 200 OK
```

### Get Relation
```http
GET /api/appbuilder/relations/{id}

Response: 200 OK
{
  "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
  "sourceEntityId": "7fa85f64-5717-4562-b3fc-2c963f66afa7",
  "targetEntityId": "bfa85f64-5717-4562-b3fc-2c963f66afab",
  "name": "orders",
  "relationType": "OneToMany",
  "cascadeDelete": false,
  "createdAt": "2026-02-11T10:00:00Z"
}
```

### List Relations by Entity
```http
GET /api/appbuilder/entities/{entityId}/relations

Response: 200 OK
[
  {
    "id": "9fa85f64-5717-4562-b3fc-2c963f66afa9",
    "targetEntityId": "bfa85f64-5717-4562-b3fc-2c963f66afab",
    "name": "orders",
    "relationType": "OneToMany",
    "cascadeDelete": false
  }
]
```

### Delete Relation
```http
DELETE /api/appbuilder/relations/{id}

Response: 204 No Content
```

---

## 5. NavigationDefinition Endpoints

### Create Navigation
```http
POST /api/appbuilder/navigations
Content-Type: application/json

{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Main Menu",
  "configurationJson": {
    "items": [
      {
        "label": "Dashboard",
        "route": "/dashboard",
        "icon": "home"
      }
    ]
  }
}

Response: 201 Created
{
  "id": "cfa85f64-5717-4562-b3fc-2c963f66afac"
}
```

### Update Navigation
```http
PUT /api/appbuilder/navigations/{id}
Content-Type: application/json

{
  "name": "Main Navigation",
  "configurationJson": {
    "items": [
      {
        "label": "Home",
        "route": "/",
        "icon": "home"
      }
    ]
  }
}

Response: 200 OK
```

### List Navigations by Application
```http
GET /api/appbuilder/applications/{applicationId}/navigations

Response: 200 OK
[
  {
    "id": "cfa85f64-5717-4562-b3fc-2c963f66afac",
    "name": "Main Menu",
    "createdAt": "2026-02-11T10:00:00Z"
  }
]
```

---

## 6. PageDefinition Endpoints

### Create Page
```http
POST /api/appbuilder/pages
Content-Type: application/json

{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Dashboard",
  "route": "/dashboard",
  "configurationJson": {
    "layout": "grid",
    "widgets": [
      {
        "type": "chart",
        "title": "Sales"
      }
    ]
  }
}

Response: 201 Created
{
  "id": "dfa85f64-5717-4562-b3fc-2c963f66afad"
}
```

### Update Page
```http
PUT /api/appbuilder/pages/{id}
Content-Type: application/json

{
  "name": "Main Dashboard",
  "route": "/",
  "configurationJson": {
    "layout": "flex",
    "widgets": []
  }
}

Response: 200 OK
```

### List Pages by Application
```http
GET /api/appbuilder/applications/{applicationId}/pages

Response: 200 OK
[
  {
    "id": "dfa85f64-5717-4562-b3fc-2c963f66afad",
    "name": "Dashboard",
    "route": "/dashboard",
    "createdAt": "2026-02-11T10:00:00Z"
  }
]
```

---

## 7. DataSourceDefinition Endpoints

### Create DataSource
```http
POST /api/appbuilder/datasources
Content-Type: application/json

{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Main Database",
  "type": "PostgreSQL",
  "configurationJson": {
    "connectionString": "Host=localhost;Database=crm",
    "poolSize": 10
  }
}

Response: 201 Created
{
  "id": "efa85f64-5717-4562-b3fc-2c963f66afae"
}
```

### Update DataSource
```http
PUT /api/appbuilder/datasources/{id}
Content-Type: application/json

{
  "name": "Primary Database",
  "configurationJson": {
    "connectionString": "Host=prod-db;Database=crm",
    "poolSize": 20
  }
}

Response: 200 OK
```

### List DataSources by Application
```http
GET /api/appbuilder/applications/{applicationId}/datasources

Response: 200 OK
[
  {
    "id": "efa85f64-5717-4562-b3fc-2c963f66afae",
    "name": "Main Database",
    "type": "PostgreSQL",
    "createdAt": "2026-02-11T10:00:00Z"
  }
]
```

---

## 8. ApplicationRelease Endpoints

### Create Release
```http
POST /api/appbuilder/applications/{applicationId}/releases
Content-Type: application/json

{
  "major": 1,
  "minor": 0,
  "patch": 0,
  "releaseNotes": "Initial release"
}

Response: 201 Created
{
  "id": "ffa85f64-5717-4562-b3fc-2c963f66afaf"
}
```

### Get Release
```http
GET /api/appbuilder/releases/{id}

Response: 200 OK
{
  "id": "ffa85f64-5717-4562-b3fc-2c963f66afaf",
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "major": 1,
  "minor": 0,
  "patch": 0,
  "version": "1.0.0",
  "releaseNotes": "Initial release",
  "isActive": true,
  "releasedAt": "2026-02-11T10:00:00Z"
}
```

### Get Release Detail (with snapshots)
```http
GET /api/appbuilder/releases/{id}/detail

Response: 200 OK
{
  "id": "ffa85f64-5717-4562-b3fc-2c963f66afaf",
  "version": "1.0.0",
  "navigationJson": "[...]",
  "pageJson": "[...]",
  "dataSourceJson": "[...]",
  "entityJson": "[...]"
}
```

### List Releases by Application
```http
GET /api/appbuilder/applications/{applicationId}/releases

Response: 200 OK
[
  {
    "id": "ffa85f64-5717-4562-b3fc-2c963f66afaf",
    "major": 1,
    "minor": 0,
    "patch": 0,
    "version": "1.0.0",
    "isActive": true,
    "releasedAt": "2026-02-11T10:00:00Z"
  }
]
```

### Activate Release
```http
POST /api/appbuilder/releases/{id}/activate

Response: 200 OK
```

### Deactivate Release
```http
POST /api/appbuilder/releases/{id}/deactivate

Response: 200 OK
```

---

## Success Criteria

- ✅ All endpoints updated to use *Definition naming
- ✅ Removed ApplicationSchema endpoints
- ✅ Added EntityDefinition endpoints
- ✅ Added PropertyDefinition endpoints
- ✅ Added RelationDefinition endpoints
- ✅ Updated ApplicationRelease endpoints (Major/Minor/Patch)
- ✅ All endpoints return proper HTTP status codes
- ✅ All endpoints use proper DTOs

---

## Authentication & Authorization

All endpoints require authentication via JWT bearer token.

**Required Permissions**:
- `appbuilder.applications.read` - Read application definitions
- `appbuilder.applications.write` - Create/update application definitions
- `appbuilder.applications.delete` - Archive application definitions
- `appbuilder.releases.write` - Create releases
- `appbuilder.components.write` - Manage components
- `appbuilder.settings.write` - Manage settings

---

## File Structure

```
AppBuilder.Api/
├── Controllers/
│   ├── ApplicationDefinitionsController.cs
│   ├── ApplicationReleasesController.cs
│   ├── NavigationComponentsController.cs
│   ├── PageComponentsController.cs
│   ├── DataSourceComponentsController.cs
│   └── ApplicationSettingsController.cs
├── Filters/
│   └── AppBuilderExceptionFilter.cs
└── DependencyInjection.cs
```

---

## Implementation Example

```csharp
[ApiController]
[Route("api/appbuilder/application-definitions")]
[Authorize]
public class ApplicationDefinitionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApplicationDefinitionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [RequirePermission("appbuilder.applications.write")]
    public async Task<IActionResult> Create([FromBody] CreateApplicationDefinitionCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpGet("{id}")]
    [RequirePermission("appbuilder.applications.read")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetApplicationDefinitionByIdQuery { ApplicationDefinitionId = id });
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    // ... other endpoints
}
```




