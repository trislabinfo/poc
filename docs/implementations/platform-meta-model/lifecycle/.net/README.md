# PlatformMetaModel.Lifecycle .NET

.NET class library generated from the [application lifecycle schema](../application-lifecycle.schema.json). Use these types for application lifecycle, catalog, tenant application, environment, and deployment artifacts.

## Structure

- **Root**: `ApplicationLifecycleDefinition` – root type with optional `ApplicationRelease`, `ApplicationCatalog`, `TenantApplication`, `TenantApplicationRelease`, `Environment`, `Deploy`, `ExtensionRelease`, `ExtensionCatalog`
- **Common**: `AuditDefinition`, `DomainModelDefinition` (shared with lifecycle definitions)
- **ApplicationRelease**: `ApplicationReleaseDefinition`, `ResolvedExtensionVersion`, `ValidationStatus`
- **ApplicationReleaseArtifact**: `ApplicationReleaseArtifactDefinition`, `ApplicationReleaseArtifactType`
- **ApplicationCatalog**: `ApplicationCatalogDefinition`
- **TenantApplication**: `TenantApplicationDefinition`, `TenantApplicationOverridesDefinition`, source/status enums
- **TenantApplicationRelease**: `TenantApplicationReleaseDefinition`
- **TenantApplicationReleaseArtifact**: `TenantApplicationReleaseArtifactDefinition`, `TenantApplicationReleaseArtifactType`
- **Environment**: `EnvironmentDefinition`, `EnvironmentType`
- **Deploy**: `DeployDefinition`, `DeployStatus`
- **ExtensionRelease**: `ExtensionReleaseDefinition`, `ResolvedExtensionDependency`
- **ExtensionReleaseArtifact**: `ExtensionReleaseArtifactDefinition`, `ExtensionReleaseArtifactType`
- **ExtensionCatalog**: `ExtensionCatalogEntryDefinition`, `ExtensionCatalogVisibility`

## Usage

```csharp
using System.Text.Json;
using PlatformMetaModel.Lifecycle;

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};
var model = JsonSerializer.Deserialize<ApplicationLifecycleDefinition>(json, options);
```

## JSON

Lifecycle JSON uses **camelCase** property names. Use `PropertyNameCaseInsensitive = true` and `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` when serializing/deserializing.
