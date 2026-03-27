# PlatformMetaModel .NET

.NET class library generated from the [application meta model schema](../application-meta-model.schema.json). Use these types to load, validate, and serialize platform/application definitions (e.g. `application/apps/*.json`).

## Structure

- **Root**: `PlatformDefinition` – root type with `Application` and optional `Persistence`
- **Application**: `ApplicationDefinition` – app container (entities, pages, extensions, themes, etc.)
- **Common**: shared types (`AuditDefinition`, `CommonPropertiesDefinition`, `DomainModelDefinition`)
- **Component**: `ComponentDefinition`, `ComponentPropertyDefinition`, `ComponentCategory` enum
- **Entity**: `EntityDefinition`, `PropertyDefinition`, `RelationDefinition`, `CalculatedFieldDefinition`
- **Extension**: `ExtensionDefinition`, `ExtensionReference`, overrides
- **Layout**: `LayoutNode` (polymorphic: Section, Row, Tabs, Tab, Field, DataTable), `ListConfig`, `FieldOverride`
- **Page**: `PageDefinition`, `PagePermissions`
- **Navigation**: `NavigationDefinition`, `NavigationItemDefinition`
- **Theme**, **Breakpoint**, **Role**, **Permission**, **Workflow**, **Validation**, **DataSource**, **CodeTable**, **Persistence**: definition types for each area

## Usage

```csharp
using System.Text.Json;
using PlatformMetaModel;

var json = await File.ReadAllTextAsync("gradbisce.json");
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true
};
var model = JsonSerializer.Deserialize<PlatformDefinition>(json, options);
```

## JSON

Application JSON uses **camelCase** property names. Use `PropertyNameCaseInsensitive = true` and `PropertyNamingPolicy = JsonNamingPolicy.CamelCase` when serializing/deserializing.
